using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class CentralResources : MonoBehaviour
{
    // I hate C#
    public Battery Battery { get; set; } = new Battery(Constants.centralBatteryCapacity, Constants.centralBatteryCapacity / 2);
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
        float dayCycle = Mathf.Sin(Time.time / Constants.dayLength * 2 * Mathf.PI);
        if (dayCycle > 0)
        {
            double old = Battery.current;
            Battery.add(Constants.peakSolarPower * dayCycle, Time.deltaTime);
            TotalBattery += (Battery.current - old);
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

