using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ANTSniffer
{
    public class cTrainerDefinition
    {
        public string name;
        public double forthOrder;
        public double thirdOrder;
        public double secondOrder;
        public double firstOrder;
    }

    
    public class cSpowerCalcs
    {
        public List<cTrainerDefinition> l_Trainers;

        public cSpowerCalcs()
        {
            l_Trainers = new List<cTrainerDefinition>();
            loadKnownTrainers();
        }

        public int getSpower(int trainerType, double mph)
        {
            if (trainerType < 0 || trainerType >= l_Trainers.Count || mph <= 0)
                return 0;
            return (int)(l_Trainers[trainerType].firstOrder * mph +
                l_Trainers[trainerType].secondOrder * Math.Pow(mph,2) +
                l_Trainers[trainerType].thirdOrder * Math.Pow(mph, 3) +
                l_Trainers[trainerType].forthOrder * Math.Pow(mph, 4));
            
        }

        private void loadKnownTrainers()
        {
            cTrainerDefinition trainer = new cTrainerDefinition();
            //y = 0.0115x3 - 0.0137x2 + 8.9788x
            //y -> Power, x->speedMPH, per 
            //http://thebikegeek.blogspot.com/2009/12/while-we-wait-for-better-and-better.html
            trainer.name = "Cyclops Fluid2";
            trainer.forthOrder = 0;
            trainer.thirdOrder = .0115;
            trainer.secondOrder = -.0137;
            trainer.firstOrder = 8.9788;

            l_Trainers.Add(trainer);

            trainer = new cTrainerDefinition();
            trainer.name = "Cyclops Fluid2";
            trainer.forthOrder = 0;
            trainer.thirdOrder = .0115;
            trainer.secondOrder = -.0137;
            trainer.firstOrder = 8.9788;

            //more from: http://www.kurtkinetic.com/calibration_chart.php
            /*  Trainer Model	A	B	Corresponding Formula
            */

            // CycleOps Fluid 2	 0747	 4669	 0.74715x + 0.0466912x3
            trainer = new cTrainerDefinition(); 
            trainer.name = "Cyclops Fluid2 Op2";
            trainer.forthOrder = 0;
            trainer.thirdOrder = .0466912;
            trainer.secondOrder = 0;
            trainer.firstOrder = 0.74715;
            l_Trainers.Add(trainer);

            // BlackBurn Fluid	 9005	 1000	 9.00528x + 0.00999636x3
            trainer = new cTrainerDefinition();
            trainer.name = "Blackburn Fluid";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = .00999636;
            trainer.firstOrder = 9.00528;
            l_Trainers.Add(trainer);

            // Elite Fluid Alu	 7113	 0886	 7.11346x + 0.00885877x3
            trainer = new cTrainerDefinition();
            trainer.name = "Elite Fluid Alu";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = 0.00885877;
            trainer.firstOrder = 7.11346;
            l_Trainers.Add(trainer);
            // Elite Volare	 4660	 1316	 4.65979x + 0.0131627x3
            trainer = new cTrainerDefinition();
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.name = "Elite Volare";
            trainer.thirdOrder = 0.0131627;
            trainer.firstOrder = 4.65979;
            l_Trainers.Add(trainer);
            // Kinetic AC Pro	 5245	 1917	 5.244820x + 0.01968x3
            trainer = new cTrainerDefinition();
            trainer.name = "Kinetic AC Pro";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = 0.01968;
            trainer.firstOrder = 5.244820;
            l_Trainers.Add(trainer);
            // Kinetic Cyclone	 6481	 2011	 6.48109x + 0.020106x3
            trainer = new cTrainerDefinition();
            trainer.name = "Kinetic Cyclone";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = 0.020106;
            trainer.firstOrder = 6.48109;
            l_Trainers.Add(trainer);
            // Kinetic Standard Fluid	 5245	 1917	 5.244820x + 0.01968x3
            trainer = new cTrainerDefinition();
            trainer.name = "Kinetic Standard Fluid";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = 0.01968;
            trainer.firstOrder = 5.244820;
            l_Trainers.Add(trainer);
            // Kinetic Road Machine	 5245	 1917	 5.244820x + 0.01968x3
            trainer = new cTrainerDefinition();
            trainer.name = "Kinetic Road Machine";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = 0.01968;
            trainer.firstOrder = 5.244820;
            l_Trainers.Add(trainer);
            // PerformanceTravelTrac Century Fluid	 4145	 1217	 4.145000x + 0.01217x3
            trainer = new cTrainerDefinition();
            trainer.name = "PerformanceTravelTrac Century Fluid";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = 0.01217;
            trainer.firstOrder = 4.145000;
            l_Trainers.Add(trainer);
            // Spinervals Super Fluid 4.5	 5245	 1917	 5.244820x + 0.01968x3
            trainer = new cTrainerDefinition();
            trainer.name = "Spinervals Super Fluid 4.5";
            trainer.forthOrder = 0;
            trainer.secondOrder = 0;
            trainer.thirdOrder = 0.01968;
            trainer.firstOrder = 5.244820;
            l_Trainers.Add(trainer);
            // Kreitler 4.5	 No Fan
            trainer = new cTrainerDefinition();
            trainer.name = "Kreitler 4.5 Drums - No Fan";
            trainer.forthOrder = 0;
            trainer.thirdOrder = -0.0021;
            trainer.secondOrder = 0.1698;
            trainer.firstOrder = 3.6497;
            l_Trainers.Add(trainer);
            // Kreitler 4.5	 No Fan
            trainer = new cTrainerDefinition();
            trainer.name = "Kreitler 4.5 Drums - Fan 1/4 Open";
            trainer.forthOrder = 0;
            trainer.thirdOrder = -0.0018;
            trainer.secondOrder = 0.4303;
            trainer.firstOrder = 3.7912;
            l_Trainers.Add(trainer);
            // Kreitler 4.5	 No Fan
            trainer = new cTrainerDefinition();
            trainer.name = "Kreitler 4.5 Drums - Fan 1/2 Open";
            trainer.forthOrder = 0;
            trainer.thirdOrder = -0.0007;
            trainer.secondOrder = 0.6764;
            trainer.firstOrder = 1.3157;
            l_Trainers.Add(trainer);
            // Kreitler 4.5	 No Fan
            trainer = new cTrainerDefinition();
            trainer.name = "Kreitler 4.5 Drums - Fan 3/4 Open";
            trainer.forthOrder = 0;
            trainer.thirdOrder = -0.0008;
            trainer.secondOrder = 0.8046;
            trainer.firstOrder = 1.2466;
            l_Trainers.Add(trainer);
            // Kreitler 4.5	 No Fan
            trainer = new cTrainerDefinition();
            trainer.name = "Kreitler 4.5 Drums - Fan Full Open";
            trainer.forthOrder = 0;
            trainer.thirdOrder = 0.0012;
            trainer.secondOrder = 0.9073;
            trainer.firstOrder = -.4064;
            l_Trainers.Add(trainer);
        }
    }
}
