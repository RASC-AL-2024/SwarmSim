public static class Constants
{
    // Power
    public const float reactorPower = 10000; // W
    public const float centralBatteryCapacity = 1e7f; // J
    public const float roverBatteryCapacity = 720000; // J (200Wh)
    public const float servoIdleDrain = 0.1f; // W, how much each servo drains when idle
    public const float servoActiveDrain = 0.5f; // W, how much a each servo drains when it moves (added to idle)
    public const float chargeRate = 8f; // W, rate at which rover batteries charge
    public const float lowBatteryThreshold = 0.3f; // 20%, when we go back to charge

    // Raw material
    public const float roverCarryingCapacity = 500; // g, how much dirt rover can carry
    public const float scoopCapacity = 100; // g, how much dirt each scoop holds
    public const float dirtTransferRate = 250; // g/s, how fast dirt moves from rover to central storage

    // Processing
    public const float centralDirtCapacity = 20000; // g, how much dirt can be stored

    public const float powderizeRate = 10; // g/s, dirt -> powder rate
    public const float powderizeDrain = 10000; // W, how much energy it costs to run powderize
    public const float powderizeYield = 0.95f; // ratio

    public const float heatRate = 10; // g/s, powder -> printable rate
    public const float heatDrain = 0; // 5000; // W
    public const float heatYield = 0.96f; // ratio

    public const float printRate = 5f;//0.3f; // g/s, printable -> spare module rate
    public const float printDrain = 20000; // W
    public const float moduleMass = 300; // g
    public const float spareModuleCapacity = 100000; // # spare modules

    // We can print at 10g/s
    // Rovers take about 400s round trip
    // Single rover does 5/4g/s
    // So need 8 rovers

    // Repair
    public const float repairTime = 60; // s to repair once another rover gets to the crime scene
    public const float maybeFailInterval = 1000; // s, how often possibility of failure
    public const float failureChance = 0f;//0.005f; // probability of failing

    public const float roverSpeed = 0.015f; // m/s (curiosity is like 0.04)

    // Fast-mode
    public const bool isFast = true; // disables RVO, and arm animations

    // Robot creation
    public const double armRoverRatio = 0.5;
    public const int NarmModules = 11;
    public const int NroverModules = 6;

    public const int targetArms = 3;
    public const int targetRovers = 8;
}

// 0.01 speed + 0.5 servo active drain gives like 250 W/m (bad)
