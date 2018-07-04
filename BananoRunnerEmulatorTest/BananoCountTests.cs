namespace BananoRunnerEmulator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class BananoCountTests
    {
        [Fact]
        public void NoBananos()
        {
            var sample = "0,0,1|0,0,1|0,0,1|0,0,1|0,0,1|2,0,0|2,0,0|2,0,0|2,0,0|2,0,0";
            var data = Parse(sample);

            Assert.Equal(0, Emulator.ComputeBananoCollected(data));
            Assert.Equal(0, Emulator.ComputeBananoMissed(data));
        }

        [Fact]
        public void SimpleBananos()
        {
            var sample = "3,0,1|4,0,1|10,0,1|0,0,1|0,0,1|2,0,0|2,0,0|2,0,0|2,0,0|2,0,0";
            var data = Parse(sample);

            Assert.Equal(3, Emulator.ComputeBananoCollected(data));
            Assert.Equal(0, Emulator.ComputeBananoMissed(data));
        }

        [Fact]
        public void MissedBananos()
        {
            var sample = "3,3,1|4,0,1|10,0,1|0,0,1|0,0,1|2,0,0|2,0,0|2,0,0|2,0,0|2,0,0";
            var data = Parse(sample);

            Assert.Equal(3, Emulator.ComputeBananoCollected(data));
            Assert.Equal(1, Emulator.ComputeBananoMissed(data));
        }

        [Fact]
        public void BonusBananos()
        {
            var sample = "3,3,1|4,0,1|10,1,1|0,0,1|0,0,1|2,0,0|2,0,0|2,0,0|2,0,0|2,0,0";
            var data = Parse(sample);

            Assert.Equal(4, Emulator.ComputeBananoCollected(data));
            Assert.Equal(1, Emulator.ComputeBananoMissed(data));
        }

        private List<int[]> Parse(string data)
        {
            return data
                .Split("|")
                .Select(x => x.Split(",").Select(y => int.Parse(y)).ToArray())
                .ToList();
        }
    }
}
