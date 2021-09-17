using JetBrains.Annotations;

using Microsoft.OpenApi.Models;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse]
    public abstract class HandlesCommandAttribute : Attribute
    {
        public const string DefaultEntityId = "entityId";

        public string Entity { get; }
        public string? EntityId { get; set; }
        public OperationType Method { get; }
        public string? Property { get; init; }
        public string? PropertyId { get; set; }

        internal string EntityIdOrDefault => EntityId ?? DefaultEntityId;

        internal string Pattern =>
            Property is not null ? SpecificPropertyPattern :
            EntityId is not null ? SpecificEntityPattern :
            BaseEntityPattern;

        private string SpecificPropertyPattern => $"{SpecificEntityPattern}/{Property}{(PropertyId is null ? "" : $"/{{{PropertyId}}}")}";
        private string SpecificEntityPattern => $"{BaseEntityPattern}/{{{EntityIdOrDefault}}}";

        private string BaseEntityPattern => $"/{Entity.ToLowerInvariant()}";

        protected HandlesCommandAttribute(string entity, OperationType method)
        {
            Entity = entity;
            Method = method;
        }
    }

    public class DeletesEntityAttribute : HandlesCommandAttribute
    {
        public DeletesEntityAttribute(string entity) : base(entity, OperationType.Delete)
        {
            EntityId = DefaultEntityId;
        }
    }

    public class AddsEntityAttribute : HandlesCommandAttribute
    {
        public AddsEntityAttribute(string entity) : base(entity, OperationType.Post) { }
    }
}
