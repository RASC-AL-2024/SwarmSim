using UnityEngine;

public class CentralResources : MonoBehaviour
{
    public Battery battery = new Battery(Constants.centralBatteryCapacity);

    public Storage dirt = new Storage(Constants.centralDirtCapacity, 0);
    public Storage powder = new Storage(Constants.centralDirtCapacity, 0);
    public Storage printMaterial = new Storage(Constants.centralDirtCapacity, 0);
    public Storage spareModules = new Storage(Constants.spareModuleCapacity, 0);

    void Start()
    {
        battery = new Battery(Constants.centralBatteryCapacity);
        dirt = new Storage(Constants.centralDirtCapacity, 0);
        powder = new Storage(Constants.centralDirtCapacity, 0);
        printMaterial = new Storage(Constants.centralDirtCapacity, 0);
        spareModules = new Storage(Constants.spareModuleCapacity, 0);
    }

    void Update()
    {
        // Solar power needs a sine weighting
        float dayCycle = Mathf.Sin(Time.time / Constants.dayLength * 2 * Mathf.PI);
        if (dayCycle > 0)
            battery.add(Constants.peakSolarPower * dayCycle, Time.deltaTime);

        if (!dirt.empty() && !powder.full())
        {
            float transferAmount = Constants.powderizeYield * dirt.remove(Constants.powderizeRate, Time.deltaTime);
            powder.add(transferAmount);
            battery.remove(Constants.powderizeDrain, Time.deltaTime);
        }

        if (!powder.empty() && !printMaterial.full())
        {
            float transferAmount = Constants.heatYield * powder.remove(Constants.heatRate, Time.deltaTime);
            printMaterial.add(transferAmount);
            battery.remove(Constants.heatDrain, Time.deltaTime);
        }

        if (!printMaterial.empty() && !spareModules.full())
        {
            float transferAmount = printMaterial.remove(Constants.printRate, Time.deltaTime) / Constants.moduleMass;
            spareModules.add(transferAmount);
            battery.remove(Constants.printDrain, Time.deltaTime);
        }
    }
}

