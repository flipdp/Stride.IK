using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Animations;

namespace IKTesting
{
    public class StartRunAnimation : StartupScript
    {
        // Declared public member fields and properties will show in the game studio

        public override void Start()
        {
            // Initialization of the script.
            Entity.Get<AnimationComponent>().Play("Run");
            //var props = typeof(AnimationProcessor).GetProperties();
            //props.Where(x => x.Name == "Order").First().SetValue(Entity.EntityManager.Processors.Get<AnimationProcessor>(),-10005);
            // return (SharpDX.Direct3D11.DeviceChild)prop.GetValue(graphicsResource);
            // Entity.EntityManager.Processors.Get<AnimationProcessor>().Order = -10005;
        }
    }
}
