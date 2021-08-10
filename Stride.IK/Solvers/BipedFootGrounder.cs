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

namespace Stride.IK.Solvers
{
    [DataContract("Biped Foot Grounder")]
    [Display("Biped Foot Grounder", Expand = ExpandRule.Once)]
    [ComponentCategory("IK")]
    public class BipedFootGrounder : IKComponent
    {
        [DataContract]
        public struct FootSelector
        {
            public string Root;
            public Entity Pole;
        }
        [DataMember("Right Foot")] public FootSelector rightFoot;
        [DataMember("Left Foot")] public FootSelector leftFoot;
        [DataMember("Foot Spacing")] public float footSpacing = 0.2f;


        private Entity lF, rF;
        private HitResult hit;

        public override void BuildGraph()
        {
            lF = new Entity("LeftFootTarget");
            lF.SetParent(Entity);
            rF = new Entity("RightFootTarget");
            rF.SetParent(Entity);

            base.BuildGraph();


            lF.Transform.Position = new Vector3(footSpacing, 0.2f, 0f);
            rF.Transform.Position = new Vector3(-footSpacing, 0.2f, 0f);

            AddChain(2, rightFoot.Root, rF, rightFoot.Pole);
            AddChain(2, leftFoot.Root, lF, leftFoot.Pole);
        }

        public override void ComputeIK(GameTime time, PhysicsProcessor physics)
        {
            if(physics != null)
            {
                hit = physics.Simulation.Raycast(lF.Transform.WorldMatrix.TranslationVector + Vector3.UnitY, lF.Transform.WorldMatrix.TranslationVector - Vector3.UnitY);
                if (hit.Succeeded)
                {
                    lF.Transform.WorldMatrix.TranslationVector = hit.Point;
                    // ROTATION
                }
                hit = physics.Simulation.Raycast(rF.Transform.WorldMatrix.TranslationVector + Vector3.UnitY, rF.Transform.WorldMatrix.TranslationVector - Vector3.UnitY);
                if(hit.Succeeded)
                {
                    rF.Transform.WorldMatrix.TranslationVector = hit.Point;
                    // ROTATION
                }
            }

            base.ComputeIK(time, physics);
        }
    }
}
