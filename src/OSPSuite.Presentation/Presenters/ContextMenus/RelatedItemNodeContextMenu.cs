﻿using System.Collections.Generic;
using System.Linq;
using OSPSuite.Assets;
using OSPSuite.Core.Diagram;
using OSPSuite.Presentation.Core;
using OSPSuite.Presentation.MenuAndBars;
using OSPSuite.Presentation.Presenters.Journal;
using OSPSuite.Utility.Container;
using OSPSuite.Utility.Extensions;

namespace OSPSuite.Presentation.Presenters.ContextMenus
{
   public class RelatedItemNodeContextMenuFactory : IContextMenuSpecificationFactory<IReadOnlyList<IBaseNode>>
   {
      private readonly IContainer _container;

      public RelatedItemNodeContextMenuFactory(IContainer container)
      {
         _container = container;
      }

      public IContextMenu CreateFor(IReadOnlyList<IBaseNode> nodes, IPresenterWithContextMenu<IReadOnlyList<IBaseNode>> presenter)
      {
         return new RelatedItemNodeContextMenu(nodes.Cast<IRelatedItemNode>().ToList(), presenter.DowncastTo<IJournalDiagramPresenter>(), _container);
      }

      public bool IsSatisfiedBy(IReadOnlyList<IBaseNode> baseNodes, IPresenterWithContextMenu<IReadOnlyList<IBaseNode>> presenter)
      {
         return baseNodes.All(baseNode => baseNode.IsAnImplementationOf<IRelatedItemNode>())
            && presenter.IsAnImplementationOf<IJournalDiagramPresenter>();
      }
   }

   public class RelatedItemNodeContextMenu : ContextMenu<IReadOnlyList<IRelatedItemNode>, IJournalDiagramPresenter>
   {
      public RelatedItemNodeContextMenu(IReadOnlyList<IRelatedItemNode> nodes, IJournalDiagramPresenter presenter, IContainer container)
         : base(nodes, presenter, container)
      {
      }

      protected override IEnumerable<IMenuBarItem> AllMenuItemsFor(IReadOnlyList<IRelatedItemNode> relatedItemNodes, IJournalDiagramPresenter presenter)
      {
         if (relatedItemNodes.Count == 2)
            yield return CreateMenuButton.WithCaption(MenuNames.Compare)
               .WithActionCommand(() => presenter.Compare(relatedItemNodes))
               .WithIcon(ApplicationIcons.SimulationComparisonFolder);

         yield return CreateMenuButton.WithCaption(MenuNames.Delete)
            .WithActionCommand(presenter.DeleteSelection)
            .WithIcon(ApplicationIcons.Delete)
            .AsGroupStarter();
      }
   }
}
