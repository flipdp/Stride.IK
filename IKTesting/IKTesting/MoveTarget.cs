using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;

namespace IKTesting
{
    public class MoveTarget : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        Vector3 initPos;
        public override void Start()
        {
            initPos = Entity.Transform.Position;
        }

        public override void Update()
        {
            var s = Game.UpdateTime.Total.TotalSeconds;
            var coss = (float)Math.Cos(s);
            var sins = (float)Math.Sin(s);
            Entity.Transform.Position = new Vector3(1,0.2f,0) + Vector3.UnitZ * sins;
        }
    }
}
