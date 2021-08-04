using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Stride.Core.Mathematics;
using System;

namespace Stride.IK
{
    public class IKProcessor : EntityProcessor<IKComponent>
    {
        public IKProcessor()
        {
            Order = 0;
        }
        public override void Draw(RenderContext context)
        {
            foreach(var c in ComponentDatas)
            {
                try
                {
                    var s = c.Key.Entity.Get<ModelComponent>().Skeleton;
                    var x = c.Key.Entity.EntityManager.Processors;
                    c.Key.ComputeFabrik(context.Time);
                    //s.NodeTransformations[21].Transform.Rotation *= Quaternion.RotationY(30 * (float)context.Time.Elapsed.TotalSeconds);
                    // s.NodeTransformations[21].Transform.Position.Y += 5;

                } catch (Exception)
                {
                    //
                }
            }
        }

        
        public override void Update(GameTime time)
        {
            base.Update(time);
        }

        protected override IKComponent GenerateComponentData(Entity entity, IKComponent component)
        {
            return base.GenerateComponentData(entity, component);
        }

        protected override bool IsAssociatedDataValid(Entity entity, IKComponent component, IKComponent associatedData)
        {
            return component.CheckValid();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] IKComponent component, [NotNull] IKComponent data)
        {
            component.BuildGraph();
            base.OnEntityComponentAdding(entity, component, data);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] IKComponent component, [NotNull] IKComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
        }

        protected override void OnSystemAdd()
        {
            base.OnSystemAdd();
        }

        protected override void OnSystemRemove()
        {
            base.OnSystemRemove();
        }

        protected override void ProcessEntityComponent(Entity entity, EntityComponent entityComponentArg, bool forceRemove)
        {
            base.ProcessEntityComponent(entity, entityComponentArg, forceRemove);
        }

        protected override void RemoveAllEntities()
        {
            base.RemoveAllEntities();
        }
    }
}