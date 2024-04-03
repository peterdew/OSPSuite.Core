using System.Collections.Generic;
using OSPSuite.Assets;
using OSPSuite.Core.Domain;
using OSPSuite.Presentation.Core;
using OSPSuite.Utility.Collections;
using OSPSuite.Utility.Extensions;
using OSPSuite.Presentation.MenuAndBars;
using IContainer = OSPSuite.Utility.Container.IContainer;

namespace OSPSuite.Presentation.Presenters.ContextMenus
{
   public interface IContainerContextMenuFactory : IContextMenuFactory<IEntity>
    {
    }

    public class ContainerContextMenuFactory : ContextMenuFactory<IEntity>, IContainerContextMenuFactory
    {
        public ContainerContextMenuFactory(IRepository<IContextMenuSpecificationFactory<IEntity>> contextMenuSpecFactoryRepository)
           : base(contextMenuSpecFactoryRepository)
        {
        }

        public bool IsSatisfiedBy(IViewItem viewItem, IPresenterWithContextMenu<IViewItem> presenter)
        {
            return viewItem.IsAnImplementationOf<IEntity>() &&
                   presenter.IsAnImplementationOf<IContainerPresenter>();
        }
    } //TODO: What is a container vs an entity? (IContainer implements IEntity)

    //public class JournalDiagramBackgroundContextMenu : ContextMenu<JournalDiagramBackground, IJournalDiagramPresenter>
    public class EntityContextMenu : ContextMenu<Entity, IEditPresenter>
    {
       public EntityContextMenu(Entity objectRequestingContextMenu, IEditPresenter context, IContainer container) : base(objectRequestingContextMenu, context, container)
       {
       }

       protected override IEnumerable<IMenuBarItem> AllMenuItemsFor(Entity objectRequestingContextMenu, IEditPresenter editPresenter)
       {
          yield return CreateMenuButton.WithCaption(MenuNames.CopyToClipboard)
             //.WithActionCommand(editPresenter.CopyToClipboard)
             .WithIcon(ApplicationIcons.Copy)
             .AsGroupStarter();
        }
    }
}