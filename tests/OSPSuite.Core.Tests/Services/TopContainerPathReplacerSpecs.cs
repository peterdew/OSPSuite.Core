using System.Collections.Generic;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Services;

namespace OSPSuite.Core.Services
{
   public abstract class concern_for_TopContainerPathReplacer : ContextSpecification<TopContainerPathReplacer>
   {
      protected string _modelName;
      protected ObjectPath _objectPath;
      private IReadOnlyList<string> _topContainerNames;

      protected override void Context()
      {
         _modelName = "toto";
         _topContainerNames = new List<string> {"root1", "root2"};
         sut = new TopContainerPathReplacer(_modelName, _topContainerNames);
      }

      protected override void Because()
      {
         sut.ReplaceIn(_objectPath);
      }
   }

   public class When_replacing_a_path_containing_a_key_word : concern_for_TopContainerPathReplacer
   {
      private string _pathAsString;

      protected override void Context()
      {
         base.Context();
         _objectPath = new ObjectPath(ObjectPathKeywords.MOLECULE, "A");
         _pathAsString = _objectPath.ToString();
      }

      [Observation]
      public void should_not_change_the_path()
      {
         _objectPath.PathAsString.ShouldBeEqualTo(_pathAsString);
      }
   }

   public class When_replacing_a_relative_path : concern_for_TopContainerPathReplacer
   {
      private string _pathAsString;

      protected override void Context()
      {
         base.Context();
         _objectPath = new ObjectPath(ObjectPath.PARENT_CONTAINER, "A");
         _pathAsString = _objectPath.ToString();
      }

      [Observation]
      public void should_not_change_the_path()
      {
         _objectPath.PathAsString.ShouldBeEqualTo(_pathAsString);
      }
   }

   public class When_replacing_a_path_that_does_not_start_with_a_key_word_or_a_relative_path_marker : concern_for_TopContainerPathReplacer
   {
      private ObjectPath _pathWithModelNameFirst;

      protected override void Context()
      {
         base.Context();
         _objectPath = new ObjectPath("root2", "B");
         _pathWithModelNameFirst = _objectPath.Clone<ObjectPath>();
         _pathWithModelNameFirst.AddAtFront(_modelName);
      }

      [Observation]
      public void should_have_added_the_model_name_to_the_path()
      {
         _objectPath.PathAsString.ShouldBeEqualTo(_pathWithModelNameFirst.PathAsString);
      }
   }
}