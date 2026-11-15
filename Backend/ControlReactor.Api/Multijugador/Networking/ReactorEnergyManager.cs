
namespace ControlReactor.Api.Multijugador.Networking;

public sealed class ReactorEnergyManager
{
    private const int DefaultMaxEnergy = 100;

    private readonly object _sync = new();

    private readonly Dictionary<int, int> _energyByMatch = new();

    public int GetEnergy(int matchId)
    {
        lock (_sync)
        {
            return GetOrCreateEnergy(matchId);
        }
    }

    public bool IsAvailable(int matchId)
    {
        lock (_sync)
        {
            return GetOrCreateEnergy(matchId) > 0;
        }
    }

    public bool TryConsumeEnergy(
        int matchId,
        out int remainingEnergy)
    {
        lock (_sync)
        {
            int currentEnergy = GetOrCreateEnergy(matchId);

            if (currentEnergy <= 0)
            {
                remainingEnergy = 0;
                return false;
            }

            remainingEnergy = currentEnergy - 1;
            _energyByMatch[matchId] = remainingEnergy;

            return true;
        }
    }

    public void RemoveMatch(int matchId)
    {
        lock (_sync)
        {
            _energyByMatch.Remove(matchId);
        }
    }

    private int GetOrCreateEnergy(int matchId)
    {
        if (!_energyByMatch.TryGetValue(
            matchId, out int energy))
        {
            energy = DefaultMaxEnergy;
            _energyByMatch[matchId] = energy;
        }

        return energy;
    }
}
