using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using System.Linq;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using System;

namespace Stride.IK
{
    [DataContract("IKComponent")]
    [DefaultEntityComponentProcessor(typeof(IKProcessor), ExecutionMode = ExecutionMode.Runtime | ExecutionMode.Thumbnail | ExecutionMode.Preview)]
    [ComponentOrder(2005)]
    [Display("FABRIK", Expand = ExpandRule.Once)]
    [ComponentCategory("IK")]
    public class IKComponent : EntityComponent
    {
        [DataMember("Number of iteration")]
        public uint NbIteration;

        public List<IKSelector> BoneToTarget = new();

        private List<IKChain> ikChains = new();
        protected SkeletonUpdater skeleton;
        public virtual void BuildGraph()
        {
            skeleton = Entity.Get<ModelComponent>().Skeleton;
            skeleton.UpdateMatrices();
            var nodes = skeleton.Nodes.Select((x, i) => new NodeData { Index = i, Name = x.Name, Parent = x.ParentIndex, Position = skeleton.NodeTransformations[i].WorldMatrix.TranslationVector }).ToList();
            foreach (var (n, root, e, pole) in BoneToTarget.Select(x => (x.Length, x.Root, x.Target, x.Pole)))
            {
                List<NodeData> ikBones = new();
                NodeData data = nodes.Where(x => x.Name == root).FirstOrDefault();
                float fullDist = 0f;
                for (int i = 0; i <= n; i++)
                {
                    skeleton.NodeTransformations[data.Index].Flags = ModelNodeFlags.EnableRender | ModelNodeFlags.OverrideWorldMatrix;
                    data.Distance = Vector3.Distance(data.Position, nodes[data.Parent].Position);
                    fullDist += data.Distance;
                    ikBones.Add(data);
                    data = nodes[data.Parent];
                }
                ikChains.Add(new IKChain { Target = e, Chain = ikBones, MaxIterations = NbIteration, FullDistance = fullDist, Pole = pole });
            }
        }
        public void ComputeIK(GameTime time)
        {
            foreach (var chain in ikChains)
            {
                chain.Compute();

                Vector3 targetPos;
                Quaternion targetRot;
                Vector3 targetScale;
                chain.Target.Transform.GetWorldTransformation(out targetPos, out targetRot, out targetScale);

                Matrix m;
                Vector3 s = Vector3.One;
                Quaternion q;
                for (int i = chain.Chain.Count - 1; i >= 0; i--)
                {
                    var link = chain.Chain[i];
                    if (i == 0)
                        q = targetRot;
                    else
                    {
                        var v = Quaternion.RotationMatrix(Matrix.LookAtRH(link.Position, chain.Chain[i - 1].Position, Vector3.UnitY));
                        v.Invert();
                        q = Quaternion.RotationYawPitchRoll(MathF.PI * 0.5f, 0f, 0f) * v;
                    }
                    Matrix.Transformation(ref s, ref q, ref link.Position, out m);
                    skeleton.NodeTransformations[link.Index].WorldMatrix = m;
                }
            }
        }
        public bool CheckValid()
        {
            var mc = Entity.Get<ModelComponent>();
            return mc != null && mc.Skeleton != null;
        }

        internal class NodeData
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public int Parent { get; set; }
            //public int Child { get; set; }
            public float Distance
            {
                get { return distance; }
                set { distance = value; }
            }
            private float distance;
            public Vector3 Position;
        }

        internal class IKChain
        {
            public Entity Target;
            public Entity Pole;
            public List<NodeData> Chain;
            public float FullDistance;
            public uint MaxIterations;

            public void Compute()
            {
                if(Vector3.DistanceSquared(Target.Transform.WorldMatrix.TranslationVector, Chain.Last().Position) >= FullDistance*FullDistance)
                {
                    Stretch();
                }
                else
                {
                    Bend();
                }

                if (Pole != null)
                {
                    for (int i = Chain.Count - 2; i > 0; i--)
                    {
                        var p = new Plane(Chain[i+1].Position, Chain[i-1].Position - Chain[i+1].Position);
                        var projectPole = Plane.Project(p, Pole.Transform.WorldMatrix.TranslationVector);
                        var projectBone = Plane.Project(p, Chain[i].Position);

                        var angle = MathF.Acos(Vector3.Dot(Vector3.Normalize(projectBone - Chain[i+1].Position), Vector3.Normalize(projectPole - Chain[i+1].Position)));
                        var cross = Vector3.Cross(projectBone - Chain[i + 1].Position, projectPole - Chain[i + 1].Position);
                        angle = angle * MathF.Sign(Vector3.Dot(p.Normal, cross));
                        //var newPos = Chain[i].Position - Chain[i + 1].Position;
                        //Quaternion.RotationAxis(p.Normal, angle).Rotate(ref newPos);
                        //Chain[i].Position = newPos + Chain[i+1].Position;
                        Chain[i].Position = QTimesV(Quaternion.RotationAxis(p.Normal, angle), Chain[i].Position - Chain[i + 1].Position) + Chain[i + 1].Position;
                    }
                }
            }
            private void Stretch()
            {
                Vector3 dir = Vector3.Normalize(Target.Transform.WorldMatrix.TranslationVector - Chain.Last().Position);
                for (int i = Chain.Count - 2; i >= 0; i--)
                    Chain[i].Position = Chain[i + 1].Position + dir * Chain[i].Distance;
            }
            
            private void Bend()
            {
                for (uint r = 0; r < MaxIterations; r++)
                {
                    //backwards
                    for (int i = 0; i < Chain.Count-1; i++)
                    {
                        if (i == 0)
                            Chain[i].Position = Target.Transform.WorldMatrix.TranslationVector;
                        else
                            Chain[i].Position = Chain[i - 1].Position + Vector3.Normalize(Chain[i].Position - Chain[i - 1].Position) * Chain[i-1].Distance;
                    }
                    //forward
                    for (int i = Chain.Count-2; i >= 0; i--)
                        Chain[i].Position = Chain[i + 1].Position + Vector3.Normalize(Chain[i].Position - Chain[i+1].Position) * Chain[i].Distance;
                
                    //break if close enough?
                }
            }

            private Vector3 QTimesV(Quaternion rotation, Vector3 point)
            {
                Vector3 vector;
                float num = rotation.X * 2f;
                float num2 = rotation.Y * 2f;
                float num3 = rotation.Z * 2f;
                float num4 = rotation.X * num;
                float num5 = rotation.Y * num2;
                float num6 = rotation.Z * num3;
                float num7 = rotation.X * num2;
                float num8 = rotation.X * num3;
                float num9 = rotation.Y * num3;
                float num10 = rotation.W * num;
                float num11 = rotation.W * num2;
                float num12 = rotation.W * num3;
                vector.X = (((1f - (num5 + num6)) * point.X) + ((num7 - num12) * point.Y)) + ((num8 + num11) * point.Z);
                vector.Y = (((num7 + num12) * point.X) + ((1f - (num4 + num6)) * point.Y)) + ((num9 - num10) * point.Z);
                vector.Z = (((num8 - num11) * point.X) + ((num9 + num10) * point.Y)) + ((1f - (num4 + num5)) * point.Z);
                return vector;
            }
        }

        [DataContract]
        public class IKSelector
        {
            public int Length;
            public string Root;
            public Entity Target;
            public Entity Pole;
        }
    }
}