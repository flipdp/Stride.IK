using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace IKTesting
{
    public class PositionBone : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        public Entity Source;
        public Model Mesh;
        private int n = 0;

        private Entity c1;
        private Entity c2;
        private Entity c3;
        private Entity c4;

        public override void Start()
        {
            // Initialization of the script.
            c1 = new Entity
            {
                new ModelComponent { Model = Mesh}
            };
            c2 = new Entity
            {
                new ModelComponent { Model = Mesh }
            };
            c3 = new Entity
            {
                new ModelComponent { Model = Mesh }
            };
            c4 = new Entity
            {
                new ModelComponent { Model = Mesh }
            };

            Entity.AddChild(c1);//Entity.AddChild(c2);
            Entity.AddChild(c3);Entity.AddChild(c4);
            c1.Get<ModelComponent>().Materials.Add(0,Content.Load<Material>("ForwardMaterial"));
            c2.Get<ModelComponent>().Materials.Add(0,Content.Load<Material>("UpMaterial"));
            c4.Get<ModelComponent>().Materials.Add(0,Content.Load<Material>("RightMaterial"));

       }

        public override void Update()
        {
            n++;
            var no = Source.Get<ModelComponent>().Skeleton.NodeTransformations[24];
            var noc = Source.Get<ModelComponent>().Skeleton.NodeTransformations[25];
            c1.Transform.Position = no.WorldMatrix.TranslationVector + Vector3.Clamp(no.WorldMatrix.Forward,-Vector3.One,Vector3.One);
            // c2.Transform.Position = no.WorldMatrix.TranslationVector + Vector3.UnitY;
            c3.Transform.Position = no.WorldMatrix.TranslationVector + Vector3.Clamp(no.WorldMatrix.Up,-Vector3.One,Vector3.One);
            c4.Transform.Position = no.WorldMatrix.TranslationVector + Vector3.Clamp(no.WorldMatrix.Right,-Vector3.One,Vector3.One);
        }
    }
}
