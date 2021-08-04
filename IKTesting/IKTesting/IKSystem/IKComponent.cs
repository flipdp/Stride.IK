using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using System.Linq;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using System;

namespace IKTesting
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
                        q = Quaternion.RotationMatrix(Matrix.LookAtRH(chain.Chain[i-1].Position, link.Position, Vector3.UnitY));
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

/*
namespace IKTesting
{
    [DataContract("IKComponent")]
    [DefaultEntityComponentProcessor(typeof(IKProcessor), ExecutionMode = ExecutionMode.Runtime | ExecutionMode.Thumbnail | ExecutionMode.Preview)]
    [Display("Inverse Kinematics", Expand = ExpandRule.Once)]
    [ComponentOrder(2005)]
    [ComponentCategory("Animation")]
    public class IKComponent : EntityComponent
    {
        private AdjacencyGraph<NodeData, Edge<NodeData>> _graph;

        private IVertexAndEdgeListGraph<NodeData, Edge<NodeData>> Graph
        {
            get { return _graph; }
        }
        private TryFunc<NodeData, IEnumerable<Edge<NodeData>>> ShortestPath;
        private List<NodeData> Leaves;

        [DataMember("Number of iteration")]
        public uint NbIteration;

        public string Root;
        public List<IKSelector> BoneToTarget = new();

        public void BuildGraph()
        {
            _graph = new();
            var sk = Entity.Get<ModelComponent>().Skeleton;
            var nodes = sk.Nodes.Select((x, i) => new NodeData { Index = i, Name = x.Name, Parent = x.ParentIndex, Node = sk.NodeTransformations[i] }).ToList();
            foreach (var n in nodes)
            {
                if (sk.Nodes.Any(x => x.ParentIndex == n.Index))
                    n.Child = sk.Nodes.Select((x, i) => (x.ParentIndex, i)).FirstOrDefault(x => n.Index == x.ParentIndex).i;
                else
                    n.Child = -1;

            }
            nodes.ForEach(
                x =>
                {
                    var parent = _graph.Vertices.FirstOrDefault(e => e.Index == x.Parent);
                    // skGraph.AddVertex(x);
                    if (parent != null)
                    {
                        _graph.AddVertex(x);
                        parent.Distance = Vector3.Distance(parent.Node.WorldMatrix.TranslationVector, x.Node.WorldMatrix.TranslationVector); //Vector3.Distance(parent.Node.WorldMatrix.TranslationVector, x.Node.WorldMatrix.TranslationVector);
                        _graph.AddEdge(new Edge<NodeData>(parent, x));
                    }
                    else
                    {
                        _graph.AddVertex(x);
                    }
                }
            );
            Leaves = _graph.Vertices.Where(x => !_graph.Edges.Any(e => e.Source.Index == x.Index)).Where(x => BoneToTarget.Select(y => y.Name).Contains(x.Name)).ToList();
            ShortestPath = Graph.ShortestPathsDijkstra(_ => 1, _graph.Vertices.First());
        }

        public void ComputeFabrik(GameTime time)
        {
            var sk = Entity.Get<ModelComponent>().Skeleton;

            // Foreach Bone strings that needs targets
            foreach (var (n, root, e) in BoneToTarget.Select(x => (x.Name, x.Root, x.Target)))
            {
                ShortestPath = Graph.ShortestPathsDijkstra(_ => 1, _graph.Vertices.First(x => x.Name == root));
                // Find Bone
                ShortestPath(Graph.Edges.Where(x => x.Target.Name == n).First().Target, out var path);

                // Compute Fabrik

                List<FabrikData> tmp1 = new(path.Count());
                // List<FabrikData> tmp2 = new(path.Count());
                // Fill tmp1
                foreach (var tmp in path)
                {
                    tmp1.Add(new FabrikData { Position = tmp.Target.Position, Distance = tmp.Target.Distance, Node = tmp.Target });
                }
                tmp1.Last().Position = e.Transform.Position;
                for (uint r = 0; r < NbIteration; r++)
                {
                    for (int i = tmp1.Count - 2; i > 0; i--)
                    {
                        tmp1[i].Position = tmp1[i + 1].Position + Vector3.Normalize(tmp1[i].Position - tmp1[i + 1].Position) * tmp1[i].Distance;
                    }
                    for (int i = 1; i < tmp1.Count; i++)
                    {
                        tmp1[i].Position = tmp1[i - 1].Position + Vector3.Normalize(tmp1[i].Position - tmp1[i - 1].Position) * tmp1[i - 1].Distance;
                    }
                }
                foreach (var np in tmp1)
                {
                    // Compute rotations for each points of the targets     
                    var bone = np.Node;

                    if (np == tmp1.Last())
                    {
                        sk.NodeTransformations[bone.Index].Transform.Rotation = e.Transform.Rotation;
                        return;
                    }

                    var no = sk.NodeTransformations[bone.Index];
                    var cno = sk.NodeTransformations[bone.Child];
                    var p = Matrix.Invert(sk.NodeTransformations[no.ParentIndex].WorldMatrix);


                    var noWP = no.WorldMatrix.TranslationVector;
                    var cpos = Vector3.Transform(cno.WorldMatrix.TranslationVector, p).XYZ();
                    var tpos = Vector3.Transform(np.Position, p).XYZ();

                    var forward = Quaternion.RotationMatrix(Matrix.LookAtRH(no.Transform.Position, tpos, no.LocalMatrix.Up));
                    var boneDir = Quaternion.Normalize(Quaternion.BetweenDirections(no.LocalMatrix.Forward, cpos - no.Transform.Position));

                    sk.NodeTransformations[bone.Index].Transform.Rotation = boneDir;
                }


            }

        }
        public static Quaternion BoneLookAt(Vector3 childWPos, Vector3 targetPos, Vector3 nodeLocalPos, Matrix nodeLocal, Matrix parentWorldInverted)
        {

            var cpos = Vector3.Transform(childWPos, parentWorldInverted).XYZ();
            var tpos = Vector3.Transform(targetPos, parentWorldInverted).XYZ();
            var tpos2 = Vector3.Transform(Vector3.One, parentWorldInverted).XYZ();
            var forward = Quaternion.RotationMatrix(Matrix.LookAtRH(nodeLocalPos, tpos2, nodeLocal.Up));
            //forward.Normalize();
            return forward;// * Quaternion.Normalize(Quaternion.Invert(Quaternion.BetweenDirections(nodeLocal.Forward,cpos - nodeLocalPos)));
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
            public float Distance { get; set; }
            public Vector3 Position
            {
                get { return Node.WorldMatrix.TranslationVector; }
            }
            public ModelNodeTransformation Node { get; set; }
        }
        internal class FabrikData
        {
            public Vector3 Position;
            // public Quaternion Rotation;
            public float Distance;
            public NodeData Node;
        }
        #endregion
    }
}
*/