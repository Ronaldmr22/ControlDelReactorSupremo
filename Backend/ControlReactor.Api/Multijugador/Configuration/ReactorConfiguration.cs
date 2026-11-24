
namespace ControlReactor.Api.Multijugador.Configuration;

public readonly record struct ReactorData(
    float X,
    float Y,
    float Radius
);

public static class ReactorConfiguration
{
    private static readonly Dictionary<int, ReactorData> Reactors = new()
    {
        { 1, new ReactorData(-13.46f, -4.49f, 1.5f) },
        { 2, new ReactorData(-8.71f, -4.81f, 1.5f) },
        { 3, new ReactorData(-2.71f, -6.81f, 1.5f) }
    };

    public static bool TryGet(
        int sceneId,
        out ReactorData reactor)
    {
        return Reactors.TryGetValue(sceneId, out reactor);
    }
}
