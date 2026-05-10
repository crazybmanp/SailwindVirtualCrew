using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailwindVirtualCrew
{
    public class SimpleSail : ICommonSailActions
    {
        private Sail realSail;
        private GPButtonRopeWinch halyardWinch;
        private GPButtonRopeWinch sheetWinch;
        private string defaultIdentifier;

        public float halyardWinchPower { get; set; }
        public float sheetWinchPower { get; private set; }
        public string FriendlyName { get; set; }

        private static float typicalPower = 25f;

        public SimpleSail(Sail realSail, GPButtonRopeWinch halyardWinch, GPButtonRopeWinch sheetWinch, string mastName)
        {
            this.realSail = realSail;
            this.halyardWinch = halyardWinch;
            this.sheetWinch = sheetWinch;
            defaultIdentifier = $"{mastName} {realSail.sailName} @{realSail.GetCurrentInstallHeight():F1}";

            halyardWinchPower = 0f;
            sheetWinchPower = 0f;
        }

        public string getSailName() => string.IsNullOrEmpty(FriendlyName) ? defaultIdentifier : FriendlyName;
        public string getDefaultIdentifier() => defaultIdentifier;

        public Sail getRealSail()
        {
            return realSail;
        }

        public GPButtonRopeWinch getHalyardWinch()
        {
            return halyardWinch;
        }

        public GPButtonRopeWinch getSheetWinch()
        {
            return sheetWinch;
        }

        public void stop()
        {
            halyardWinchPower = 0f;
            sheetWinchPower = 0f;
        }

        public void deploySail()
        {
            halyardWinchPower = -typicalPower;
        }

        public void reefSail()
        {
            halyardWinchPower = typicalPower;
        }

        public void easeSail()
        {
            sheetWinchPower = -typicalPower;
        }

        public void trimSail()
        {
            sheetWinchPower = typicalPower;
        }
    }
}
