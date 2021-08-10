using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using System.Linq;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using System;

namespace Stride.IK.Solver
{
    [DataContract("FABRIK")]
    [Display("FABRIK", Expand = ExpandRule.Once)]
    [ComponentCategory("IK")]
    public class FABRIK : IKComponent
    {
        [DataContract]
        public struct IKSelector
        {
            public int Length;
            public string Root;
            public Entity Target;
            public Entity Pole;
        }
        public List<IKSelector> BoneToTarget = new();
        public override void BuildGraph()
        {
            base.BuildGraph();
            foreach (var (n, root, e, pole) in BoneToTarget.Select(x => (x.Length, x.Root, x.Target, x.Pole)))
                AddChain(n, root, e, pole);
        }
    }
}