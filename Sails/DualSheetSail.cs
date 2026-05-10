using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailwindVirtualCrew
{
    public class DualSheetSail : ICommonSailActions
    {
        public enum DualSheetSailSubtype { Square, Jib }

        private Sail realSail;
        private GPButtonRopeWinch halyardWinch;
        private GPButtonRopeWinch portSheetWinch;
        private GPButtonRopeWinch starboardSheetWinch;
        private DualSheetSailSubtype sailSubtype;
        private string defaultIdentifier;

        public float halyardWinchPower { get; set; }
        public float portSheetWinchPower { get; set; }
        public float starboardSheetWinchPower { get; set; }
        public string FriendlyName { get; set; }

        private static float typicalPower = 25f;

        public DualSheetSail(Sail realSail, GPButtonRopeWinch halyardWinch, GPButtonRopeWinch portSheetWinch, GPButtonRopeWinch starboardSheetWinch, DualSheetSailSubtype sailSubtype, string mastName)
        {
            this.realSail = realSail;
            this.halyardWinch = halyardWinch;
            this.portSheetWinch = portSheetWinch;
            this.starboardSheetWinch = starboardSheetWinch;
            defaultIdentifier = $"{mastName} {realSail.sailName} @{realSail.GetCurrentInstallHeight():F1}";

            halyardWinchPower = 0f;
            portSheetWinchPower = 0f;
            starboardSheetWinchPower = 0f;
            this.sailSubtype = sailSubtype;
        }

        public string getSailName() => string.IsNullOrEmpty(FriendlyName) ? defaultIdentifier : FriendlyName;
        public string getDefaultIdentifier() => defaultIdentifier;

        public Sail getRealSail()
        {
            return realSail;
        }

        public DualSheetSailSubtype getSubtype()
        {
            return sailSubtype;
        }

        public GPButtonRopeWinch getHalyardWinch()
        {
            return halyardWinch;
        }

        public GPButtonRopeWinch getPortSheetWinch()
        {
            return portSheetWinch;
        }

        public GPButtonRopeWinch getStarboardSheetWinch()
        {
            return starboardSheetWinch;
        }

        public void stop()
        {
            halyardWinchPower = 0f;
            portSheetWinchPower = 0f;
            starboardSheetWinchPower = 0f;
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
            portSheetWinchPower = -typicalPower;
            starboardSheetWinchPower = -typicalPower;
        }

        public void trimSail()
        {
            portSheetWinchPower = typicalPower;
            starboardSheetWinchPower = typicalPower;
        }

        public void bringToPort()
        {
            portSheetWinchPower = typicalPower;
            starboardSheetWinchPower = -typicalPower;
        }

        public void bringToStarboard()
        {
            portSheetWinchPower = -typicalPower;
            starboardSheetWinchPower = typicalPower;
        }
    }
}
