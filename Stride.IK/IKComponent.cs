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
    [Display("Inverse Kinematics", Expand = ExpandRule.Once)]
    [ComponentOrder(2005)]
    [ComponentCategory("Animation")]
    public class IKComponent : EntityComponent
    {
        [DataMember("Number of iteration")]
        public uint NbIteration;

        public List<IKSelector> BoneToTarget = new();
        private List<IKChain> ikChains = new();
        private SkeletonUpdater sk;

        public void BuildGraph()
        {
            sk = Entity.Get<ModelComponent>().Skeleton;
            sk.UpdateMatrices();
            var nodes = sk.Nodes.Select((x, i) => new NodeData { Index = i, Name = x.Name, Parent = x.ParentIndex, Position = sk.NodeTransformations[i].WorldMatrix.TranslationVector, InitRot = sk.NodeTransformations[i].Transform.Rotation }).ToList();
            for(int i = 0; i < nodes.Count; i++)
            {
                if (sk.Nodes.Any(x => x.ParentIndex == nodes[i].Index))
                    nodes[i].Child = sk.Nodes.Select((x, i) => (x.ParentIndex, i)).FirstOrDefault(x => nodes[i].Index == x.ParentIndex).i;
                else
                    nodes[i].Child = -1;
            }
            foreach (var (n, root, e) in BoneToTarget.Select(x => (x.Length, x.Root, x.Target)))
            {
                List<NodeData> ikBones = new();
                NodeData data = nodes.Where(x => x.Name == root).FirstOrDefault();
                float fullDist = 0f;
                for(int i = 0; i <= n; i++)
                {
                    sk.NodeTransformations[data.Index].Flags = ModelNodeFlags.EnableRender | ModelNodeFlags.OverrideWorldMatrix;
                    data.Distance = Vector3.Distance(data.Position, nodes[data.Parent].Position);
                    fullDist += data.Distance;
                    ikBones.Add(data);
                    data = nodes[data.Parent];
                }
                ikChains.Add(new IKChain { Target = e, Chain = ikBones, MaxIterations = NbIteration, FullDistance = fullDist });
            }
        }

        public void ComputeFabrik(GameTime time)
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
                for(int i = chain.Chain.Count-1; i >= 0; i--)
                {
                    var link = chain.Chain[i];
                    if (i == 0)
                        q = targetRot;
                    else
                    {                        
                        var v = Quaternion.RotationMatrix(Matrix.LookAtRH(link.Position, chain.Chain[i - 1].Position, Vector3.UnitY));
                        v.Invert();
                        q = Quaternion.RotationYawPitchRoll( MathF.PI * 0.5f, 0f, 0f ) * v;
                    }
                    Matrix.Transformation(ref s, ref q, ref link.Position, out m);
                    sk.NodeTransformations[link.Index].WorldMatrix = m;
                }
            }
        }

        public bool CheckValid()
        {
            var mc = Entity.Get<ModelComponent>();
            return mc != null && mc.Skeleton != null;
        }

        #region InverseKinematicsData

        internal class NodeData
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public int Parent { get; set; }
            public int Child { get; set; }
            public float Distance
            {
                get { return distance; }
                set { distance = value; }
            }
            private float distance;
            public Vector3 Position;
            public Quaternion InitRot;
        }

        internal class IKChain
        {
            public Entity Target;
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
                }
            }
        }

        [DataContract]
        public class IKSelector
        {
            public int Length;
            public string Root;
            public Entity Target;
        }
        #endregion
    }
}