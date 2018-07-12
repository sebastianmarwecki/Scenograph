using System.Collections.Generic;
using System.Linq;
using Packer.Packer;

namespace Packer
{
    public enum PlacementScoreRequirement
    {
        DontCare,
        PreferBest,
        LeastDamageThenBestWall,
        OnlyBestWall,
    }
}
