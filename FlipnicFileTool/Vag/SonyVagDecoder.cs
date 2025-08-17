namespace FlipnicFileTool.Vag
{
    // Originally from: https://github.com/eurotools/es-ps2-vag-tool
    // basically just the bare minimum to get Sony VAG support going
    
    public static partial class SonyVag
    {
        
        private static readonly double[,] VagLutDecoder = new[,]
        {
            {0.0, 0.0},
            {60.0 / 64.0, 0.0},
            {115.0 / 64.0, -52.0 / 64.0},
            {98.0 / 64.0, -55.0 / 64.0},
            {122.0 / 64.0, -60.0 / 64.0}
        };

        
        public static byte[] Decode(byte[] vagData)
        {
            using var vagReader = new BinaryReader(new MemoryStream(vagData, false));
            using var pcmStream = new MemoryStream();
            using var pcmWriter = new BinaryWriter(pcmStream);
            double hist1 = 0.0, hist2 = 0.0;

            //Skip header
            vagReader.BaseStream.Seek(16, SeekOrigin.Begin);

            //Start decoding
            while (vagReader.BaseStream.Position < vagReader.BaseStream.Length)
            {
                //Read chunk data
                var decodingCoefficent = vagReader.ReadByte();
                var vc = new VagChunk
                {
                    Shift = (sbyte)(decodingCoefficent & 0xF),
                    Predict = (sbyte)((decodingCoefficent & 0xF0) >> 4),
                    Flags = vagReader.ReadByte(),
                    Sample = vagReader.ReadBytes(14)
                };

                if (vc.Flags == (byte)VagFlag.VagfPlaybackEnd)
                {
                    break;
                }

                if(vc.Flags == (byte)VagFlag.VagfLoopStart)
                {
                    var sample = pcmStream.Length / 2;
                }
                else
                {
                    var samples = new int[VagSampleNibbl];

                    // expand 4bit -> 8bit
                    for (var j = 0; j < VagSampleBytes; j++)
                    {
                        samples[j * 2] = vc.Sample[j] & 0xF;
                        samples[j * 2 + 1] = (vc.Sample[j] & 0xF0) >> 4;
                    }

                    //Decode samples
                    for (var j = 0; j < VagSampleNibbl; j++)
                    {
                        // shift 4 bits to top range of int16_t
                        var s = samples[j] << 12;
                        if ((s & 0x8000) != 0)
                        {
                            s = (int)(s | 0xFFFF0000);
                        }

                        /* swy: don't overflow the LUT array access; limit the max allowed index */
                        var predict = Math.Min(vc.Predict, (sbyte)(VagLutDecoder.GetLength(0) - 1));

                        var sample = (s >> vc.Shift) + hist1 * VagLutDecoder[predict, 0] + hist2 * VagLutDecoder[predict, 1];
                        hist2 = hist1;
                        hist1 = sample;

                        pcmWriter.Write((short)(Math.Min(short.MaxValue, Math.Max(sample, short.MinValue))));
                    }
                }
            }
            var pcmData = pcmStream.ToArray();

            pcmWriter.Close();
            pcmStream.Close();
            vagReader.Close();

            return pcmData;
        }
    }
    
}