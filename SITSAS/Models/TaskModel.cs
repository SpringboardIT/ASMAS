using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class TaskModel
    {
        public SortedList<Guid, string> ExistingTasks { get; set; }
        public AccessRights rights { get; set; }
    }
    public class CreateUpdateTaskModel
    {
        public bool IsPart { get; set; }

        public bool IsFromQuestionnaire { get; set; }
        public bool TaskExists { get; set; }
        public Task ExistingTask { get; set; }
        public AccessRights rights { get; set; }
        public List<DirectoryUser> AllUsers { get; set; }
        public List<TaskStatu> AllTaskStatuses { get; set; }
    }
    public class MyTasksModel
    {
        public int NumberOfTasks { get; set; }
        public string Colour { get; set; }
    }
    public class GetTasksModel
    {
        public List<Task> Tasks { get; set; }
    }
}