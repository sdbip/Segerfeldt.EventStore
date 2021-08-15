namespace Segerfeldt.EventStore.Source
{
    /// <summary>An event that hasn't been published yet</summary>
    /// The event should include all the details needed to unambiguously
    /// define the new state of an entity (given an unambiguous prior state).
    /// <example>
    /// A Student entity might enroll in a course:
    /// <code>
    /// {
    ///   Name = "Enrolled";
    ///   Details = {CourseId = "CS193p"}
    /// }
    /// </code>
    /// </example>
    public sealed class UnpublishedEvent
    {
        /// <summary>What aspect of the entity changes</summary>
        /// <example><c>"Enrolled"</c></example>
        /// The name defines an aspect of the entity that should change.
        /// This is the typical property to filter on when
        /// selecting what state information to project.
        public string Name { get; }

        /// <summary>Details regarding the change</summary>
        /// <example><c>new {CourseId = "CS193p"}</c></example>
        public object Details { get; }

        /// <param name="name">what aspect of the entity is changing</param>
        /// <param name="details">details regarding the change</param>
        /// <example><c>
        /// new (
        ///   name: "Enrolled",
        ///   details: new {CourseId = "CS193p"}
        /// )
        /// </c></example>
        public UnpublishedEvent(string name, object details)
        {
            Name = name;
            Details = details;
        }
    }
}
