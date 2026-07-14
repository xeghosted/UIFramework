using Xunit;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// SkinManager ist statischer Zustand. xUnit führt Testklassen parallel aus —
    /// ohne diese Collection würden sich Tests gegenseitig den aktiven Skin
    /// unter den Füßen wegziehen. Jede Testklasse, die SkinManager.Current
    /// anfasst, MUSS mit [Collection(SkinManagerCollection.Name)] markiert sein.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class SkinManagerCollection
    {
        public const string Name = "SkinManager";
    }
}
