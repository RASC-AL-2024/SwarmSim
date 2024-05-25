using UnityEngine;
using System.Collections;
using System.IO;

public class CentralResources : MonoBehaviour
{
    // I hate C#
    public Battery Battery { get; set; } = new Battery(Constants.centralBatteryCapacity, Constants.centralBatteryCapacity);
    public static Storage TotalDirt { get; set; } = new Storage(double.MaxValue, 0); // may the lord forgive me
    public double TotalBattery = 0.0f; // same
    public Storage Dirt { get; set; } = new Storage(Constants.centralDirtCapacity, 0);
    public Storage Powder { get; set; } = new Storage(Constants.centralDirtCapacity, 0);
    public Storage PrintMaterial { get; set; } = new Storage(Constants.centralDirtCapacity, 0);
    public Storage SpareModules { get; set; } = new Storage(Constants.spareModuleCapacity, 0);
    public double TotalModules = 0.0f; // same
    void Start()
    {
        StartCoroutine(Log());
    }

    IEnumerator Log()
    {
        var writer = new StreamWriter("resourceData.csv");
        writer.WriteLine("time,battery,totalGenerated,spareModules,totalDirt,totalModules");
        var nextTime = Time.time;
        while (true)
        {
            nextTime += 60;
            writer.WriteLine("{0},{1},{2},{3},{4},{5}", Time.time, Battery.current, TotalBattery, SpareModules.current, TotalDirt.current, TotalModules);
            writer.Flush();
            yield return new WaitForSeconds(nextTime - Time.time);
        }
    }

    void Update()
    {
        // Solar power needs a sine weighting
        double oldBat = Battery.current;
        Battery.add(Constants.reactorPower, Time.deltaTime);
        TotalBattery += (Battery.current - oldBat);

        if (!Dirt.empty() && !Powder.full())
        {
            var desired = System.Math.Min(Dirt.current / (Constants.powderizeRate * Time.deltaTime), 1.0);
            if (desired > 0.01)
            {
                var ratio = Battery.budgetRemove(desired * Constants.powderizeDrain, Time.deltaTime, 0.2);
                Dirt.transferTo(Powder, ratio * desired * Constants.powderizeRate, Time.deltaTime, Constants.powderizeYield);
            }
        }

        if (!Powder.empty() && !PrintMaterial.full())
        {
            var desired = System.Math.Min(Powder.current / (Constants.heatRate * Time.deltaTime), 1.0);
            // var ratio = Battery.budgetRemove(desired * Constants.heatDrain, Time.deltaTime, 0.2);
            Powder.transferTo(PrintMaterial, desired * Constants.heatRate, Time.deltaTime, Constants.heatYield);
        }

        if (!PrintMaterial.empty() && !SpareModules.full())
        {
            var desired = System.Math.Min(PrintMaterial.current / (Constants.printRate * Time.deltaTime), 1.0);
            if (desired > 0.01)
            {
                var ratio = Battery.budgetRemove(desired * Constants.printDrain, Time.deltaTime, 0.2);
                double old = SpareModules.current;
                PrintMaterial.transferTo(SpareModules, ratio * desired * Constants.printRate, Time.deltaTime, 1 / Constants.moduleMass);
                TotalModules += (SpareModules.current - old);
            }
        }
    }
}

