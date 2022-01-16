﻿using System.Collections.Generic;
using System.Linq;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;

namespace OSPSuite.Core
{
   public abstract class concern_for_EventBuilderTask : ContextForModelConstructorIntegration
   {
      protected CreationResult _creationResult;
   }

   public class When_creating_simulation_with_many_floating_molecules_and_nested_applications_and_nonapplication_eventgroups : concern_for_EventBuilderTask
   {
      private List<IEventGroup> _allEventGroups;
      private const string C1 = "C1";
      private const string C2 = "C2";
      private const string C3 = "C3";
      private const string AppC2 = "AppC2";
      private const string AppC3 = "AppC3";

      public override void GlobalContext()
      {
         base.GlobalContext();
         _creationResult = CreateFrom("EventsAppKeywordReplacement");
         _allEventGroups = _creationResult.Model.Root.GetAllChildren<IEventGroup>().ToList();
      }

      [Observation]
      public void should_replace_keywords_in_applications_with_molecule_name_of_parent_application()
      {
         foreach (var eventGroup in _allEventGroups)
         {
            var param = eventGroup.GetSingleChildByName<IParameter>("P1");
            var lastPathEntry = param.Formula.ObjectPaths.ToArray()[0].Last();

            if (eventGroup.Name.StartsWith(AppC2))
            {
               lastPathEntry.ShouldBeEqualTo(C2);
            }
            else if (eventGroup.Name.StartsWith(AppC3))
            {
               lastPathEntry.ShouldBeEqualTo(C3);
            }
         }
      }
   }

   public class When_creating_simulation_using_an_event_with_a_keyword_all_floating : concern_for_EventBuilderTask
   {
      private List<IEventGroup> _allEventGroups;

      public override void GlobalContext()
      {
         base.GlobalContext();
         _creationResult = CreateFrom("simulation_with_urine_emptying");
         _allEventGroups = _creationResult.Model.Root.GetAllChildren<IEventGroup>().ToList();
      }

      [Observation]
      public void should_replace_keyword_with_the_name_of_all_floating_molecules()
      {
         var urineEmptyingEventGroup = _allEventGroups.FindByName("PIPI_1");
         var urineEmptyingStartEvent = urineEmptyingEventGroup.Events.FindByName("Urine_Emptying_StartEvent");
         urineEmptyingStartEvent.Assignments.Count().ShouldBeEqualTo(2);
         urineEmptyingStartEvent.Assignments.Find(x => x.ObjectPath.PathAsString.Contains("C1")).ShouldNotBeNull();
         urineEmptyingStartEvent.Assignments.Find(x => x.ObjectPath.PathAsString.Contains("C2")).ShouldNotBeNull();
      }
   }
}