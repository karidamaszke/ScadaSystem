using System.Collections.Generic;

namespace ScadaProject
{
    class ControlSystem
    {
        AnalogInput tempOutsideSensor = new AnalogInput(10, 0);
        AnalogInput tempVentilationAir = new AnalogInput(12, 0);
        AnalogInput tempExhaustAir = new AnalogInput(18, 0);

        DigitalInput heatingCoilThermostat = new DigitalInput(100, 0, 0);
        DigitalInput pressureSwithVentilation = new DigitalInput(101, 0, 0);
        DigitalInput pressureSwitchExhaust = new DigitalInput(101, 0, 4);

        AnalogOutput actuatorOfInitialHeater = new AnalogOutput(200, 0);
        AnalogOutput actuatorOfSecondaryHeater = new AnalogOutput(201, 0);
        AnalogOutput angularSpeedOfVentilationFan = new AnalogOutput(210, 0);
        AnalogOutput angularSpeedOfExhaustFan = new AnalogOutput(211, 0);
        AnalogOutput throttleOfCrossFlowHeatExchanger = new AnalogOutput(223, 0);

        DigitalOutput actuatorOfVentilationThrottle = new DigitalOutput(300, 0, 0);
        DigitalOutput actuatorOfExhaustThrottle = new DigitalOutput(300, 0, 5);
        DigitalOutput startInitialHeaterPump = new DigitalOutput(301, 0, 0);
        DigitalOutput startSecondaryHeaterPump = new DigitalOutput(301, 0, 1);
        DigitalOutput startVentilationFan = new DigitalOutput(302, 0, 0);
        DigitalOutput startExhaustFan = new DigitalOutput(302, 0, 1);

        public List<IoDevice> devices = new List<IoDevice>();
        
        public ControlSystem()
        {
            devices.Add(tempOutsideSensor);
            devices.Add(tempVentilationAir);
            devices.Add(tempExhaustAir);
            devices.Add(heatingCoilThermostat);
            devices.Add(pressureSwithVentilation);
            devices.Add(pressureSwitchExhaust);
            devices.Add(actuatorOfInitialHeater);
            devices.Add(actuatorOfSecondaryHeater);
            devices.Add(angularSpeedOfVentilationFan);
            devices.Add(angularSpeedOfExhaustFan);
            devices.Add(throttleOfCrossFlowHeatExchanger);
            devices.Add(actuatorOfVentilationThrottle);
            devices.Add(actuatorOfExhaustThrottle);
            devices.Add(startInitialHeaterPump);
            devices.Add(startSecondaryHeaterPump);
            devices.Add(startVentilationFan);
            devices.Add(startExhaustFan);
        }
    }
}
