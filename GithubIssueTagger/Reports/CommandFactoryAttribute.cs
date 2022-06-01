using System;

namespace GithubIssueTagger.Reports
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class CommandFactoryAttribute : Attribute
    {
        public CommandFactoryAttribute(Type factoryType)
        {
            FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
        }

        public Type FactoryType { get; }
    }
}
