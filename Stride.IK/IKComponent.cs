using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using System.Linq;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using System;
using Stride.Physics;

namespace Stride.IK
{
    [DataContract("IKComponent")]
    [DefaultEntityComponentProcessor(typeof(IKProcessor), ExecutionMode = ExecutionMode.Runtime | ExecutionMode.Thumbnail | ExecutionMode.Preview)]
    [ComponentOrder(2005)]
    //[Display("FABRIK", Expand = ExpandRule.Once)]
    //[ComponentCategory("IK")]
    public class IKComponent : EntityComponent
    {
        [DataMember("Number of iteration")]
        public uint NbIteration;

        private List<IKChain> ikChains = new();
        protected SkeletonUpdater skeleton;
        protected List<NodeData> boneNodes;
        public virtual void BuildGraph()
        {
            skeleton = Entity.Get<ModelComponent>().Skeleton;
            skeleton.UpdateMatrices();
            boneNodes = skeleton.Nodes.Select((x, i) => new NodeData { Index = i, Name = x.Name, Parent = x.ParentIndex, Position = skeleton.NodeTransformations[i].WorldMatrix.TranslationVector }).ToList();
        }

        protected void AddChain(int n, string root, Entity e, Entity pole)
        {
            List<NodeData> ikBones = new();
            NodeData data = boneNodes.Where(x => x.Name == root).FirstOrDefault();
            float fullDist = 0f;
            for (int i = 0; i <= n; i++)
            {
                skeleton.NodeTransformations[data.Index].Flags = ModelNodeFlags.EnableRender | ModelNodeFlags.OverrideWorldMatrix;
                data.Distance = Vector3.Distance(data.Position, boneNodes[data.Parent].Position);
                fullDist += data.Distance;
                ikBones.Add(data);
                data = boneNodes[data.Parent];
            }
            ikChains.Add(new IKChain { Target = e, Chain = ikBones, MaxIterations = NbIteration, FullDistance = fullDist, Pole = pole });
        }

        protected Vector3 _scale = Vector3.One;
        Quaternion _boneRot;
        public virtual void ComputeIK(GameTime time, PhysicsProcessor physics)
        {
            foreach (var chain in ikChains)
            {
                chain.Compute();

                chain.Target.Transform.GetWorldTransformation(out _, out Quaternion targetRot, out _);

                for (int i = chain.Chain.Count - 1; i >= 0; i--)
                {
                    var link = chain.Chain[i];
                    if (i == 0)
                        _boneRot = targetRot;
                    else
                    {
                        var v = Quaternion.RotationMatrix(Matrix.LookAtRH(link.Position, chain.Chain[i - 1].Position, Vector3.UnitY));
                        v.Invert();
                        _boneRot = Quaternion.RotationYawPitchRoll(MathF.PI * 0.5f, 0f, 0f) * v;
                    }
                    Matrix.Transformation(ref _scale, ref _boneRot, ref link.Position, out Matrix m);
                    skeleton.NodeTransformations[link.Index].WorldMatrix = m;
                }
            }
        }
        public virtual bool CheckValid()
        {
            var mc = Entity.Get<ModelComponent>();
            return mc != null && mc.Skeleton != null;
        }

        public class NodeData
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public int Parent { get; set; }
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

            private Vector3 dir;

            public void Compute()
            {
                if (Vector3.DistanceSquared(Target.Transform.WorldMatrix.TranslationVector, Chain.Last().Position) >= FullDistance * FullDistance)
                    Stretch();
                else
                    Bend();

                if (Pole != null)
                    ConstrainPole();
            }
            private void Stretch()
            {
                dir = Vector3.Normalize(Target.Transform.WorldMatrix.TranslationVector - Chain.Last().Position);
                for (int i = Chain.Count - 2; i >= 0; i--)
                    Chain[i].Position = Chain[i + 1].Position + dir * Chain[i].Distance;
            }

            private void Bend()
            {
                for (uint r = 0; r < MaxIterations; r++)
                {
                    //backwards
                    for (int i = 0; i < Chain.Count - 1; i++)
                    {
                        if (i == 0)
                            Chain[i].Position = Target.Transform.WorldMatrix.TranslationVector;
                        else
                            Chain[i].Position = Chain[i - 1].Position + Vector3.Normalize(Chain[i].Position - Chain[i - 1].Position) * Chain[i - 1].Distance;
                    }
                    //forward
                    for (int i = Chain.Count - 2; i >= 0; i--)
                        Chain[i].Position = Chain[i + 1].Position + Vector3.Normalize(Chain[i].Position - Chain[i + 1].Position) * Chain[i].Distance;

                    //break if close enough?
                    //Probably too expensive for two iterations
                }
            }

            private void ConstrainPole()
            {
                var polePos = Pole.Transform.WorldMatrix.TranslationVector;
                for (int i = Chain.Count - 2; i > 0; i--)
                {
                    var sphere1 = (Chain[i + 1].Position, Chain[i].Distance);
                    var sphere2 = (Chain[i - 1].Position, Chain[i - 1].Distance);
                    var intersection = SphereSphereIntersection(sphere1, sphere2);

                    var poleDir = Vector3.Normalize(Chain[i + 1].Position - polePos);
                    // Garbage to make orthogonal
                    var upDir = Vector3.Normalize(Vector3.Cross(intersection.normal, poleDir));
                    var bendDir = Vector3.Normalize(Vector3.Cross(intersection.normal, upDir));
                    Chain[i].Position = intersection.center + bendDir * intersection.radius;
                }
            }

            private (Vector3 center, Vector3 normal, float radius) SphereSphereIntersection(in (Vector3 position, float radius) sphere1, in (Vector3 position, float radius) sphere2)
            {
                float d = Vector3.Distance(sphere1.position, sphere2.position);
                float x = (d * d + sphere1.radius * sphere1.radius - sphere2.radius * sphere2.radius) * (d == 0f ? 0f : 1f / (2f * d));
                Vector3 normal = Vector3.Normalize(sphere2.position - sphere1.position);
                return (center: sphere1.position + normal * x,
                    normal: normal,
                    radius: MathF.Sqrt(MathF.Abs(sphere1.radius * sphere1.radius - x * x)));
            }
        }
    }
}