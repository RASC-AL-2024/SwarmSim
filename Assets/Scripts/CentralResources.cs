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

        // Don't print and stuff if low on energy
        if (Battery.ratio() <= 0.2)
        {
            return;
        }

        if (!Dirt.empty() && !Powder.full())
        {
            Dirt.transferTo(Powder, Constants.powderizeRate, Time.deltaTime, Constants.powderizeYield);
            Battery.remove(Constants.powderizeDrain, Time.deltaTime);
        }

        if (!Powder.empty() && !PrintMaterial.full())
        {
            Powder.transferTo(PrintMaterial, Constants.heatRate, Time.deltaTime, Constants.heatYield);
            Battery.remove(Constants.heatDrain, Time.deltaTime);
        }

        if (!PrintMaterial.empty() && !SpareModules.full())
        {
            double old = SpareModules.current;
            PrintMaterial.transferTo(SpareModules, Constants.printRate, Time.deltaTime, 1 / Constants.moduleMass);
            TotalModules += (SpareModules.current - old);
            Battery.remove(Constants.printDrain, Time.deltaTime);
        }
    }
}

