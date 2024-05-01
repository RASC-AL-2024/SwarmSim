public static class Constants
{
    // Power
    public const float peakSolarPower = 2000; // W
    public const float dayLength = 4 * 60 * 60; // s
    public const float centralBatteryCapacity = 1e6f; // J
    public const float roverBatteryCapacity = 1e4f; // J
    public const float servoIdleDrain = 0.1f; // W, how much each servo drains when idle
    public const float servoActiveDrain = 0.5f; // W, how much a each servo drains when it moves (added to idle)
    public const float chargeRate = 0.3f; // W, rate at which rover batteries charge
    public const float lowBatteryThreshold = 0.2f; // 20%, when we go back to charge

    // Raw material
    public const float roverCarryingCapacity = 1000; // g, how much dirt rover can carry
    public const float scoopCapacity = 100; // g, how much dirt each scoop holds
    public const float dirtTransferRate = 1000; // g/s, how fast dirt moves from rover to central storage

    // Processing
    public const float centralDirtCapacity = 1000; // g, how much dirt can be stored

    public const float powderizeRate = 10; // g/s, dirt -> powder rate
    public const float powderizeDrain = 600; // W, how much energy it costs to run powderize
    public const float powderizeYield = 0.96f; // ratio

    public const float heatRate = 10; // g/s, powder -> printable rate
    public const float heatDrain = 10; // W
    public const float heatYield = 0.96f; // ratio

    public const float printRate = 0.03f; // g/s, printable -> spare module rate
    public const float printDrain = 200; // W
    public const float moduleMass = 300; // g
    public const float spareModuleCapacity = 10; // # spare modules

    // Repair
    public const float repairTime = 60; // s to repair once another rover gets to the crime scene
    public const float maybeFailInterval = 10000; // s, how often possibility of failure
    public const float failureChance = 0.10f; // s, probability of failing
}
