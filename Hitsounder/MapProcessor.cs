using OsuParsers.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;
using OsuParsers.Enums.Beatmaps;
using NAudio.Wave.SampleProviders;
using System.IO;
using System.Reflection;
using MahApps.Metro.Controls;
using NAudio.Vorbis;
using NAudio;

namespace Hitsounder
{
    internal static class MapProcessor
    {
        [SuppressMessage("ReSharper", "UnusedMember.Global")] // dealt with by reflection
        internal static class Hitsounds
        {
            internal static string DrumClap = "hitsounds/drum-hitclap.wav";
            internal static string DrumFinish = "hitsounds/drum-hitfinish.wav";
            internal static string DrumNormal = "hitsounds/drum-hitnormal.wav";
            internal static string DrumWhistle = "hitsounds/drum-hitwhistle.wav";
            internal static string NormalClap = "hitsounds/normal-hitclap.wav";
            internal static string NormalFinish = "hitsounds/normal-hitfinish.wav";
            internal static string NormalNormal = "hitsounds/normal-hitnormal.wav";
            internal static string NormalWhistle = "hitsounds/normal-hitwhistle.wav";
            internal static string SoftClap = "hitsounds/soft-hitclap.wav";
            internal static string SoftFinish = "hitsounds/soft-hitfinish.wav";
            internal static string SoftNormal = "hitsounds/soft-hitnormal.wav";
            internal static string SoftWhistle = "hitsounds/soft-hitwhistle.wav";
        }

        public static void Hitsound(string mapPath, string skinPath)
        {
            LoadHitsounds(skinPath);

            Beatmap map;
            try
            {
                map = OsuParsers.Decoders.BeatmapDecoder.Decode(mapPath);
            }
            catch
            {
                MessageBox.Show(
                    "Failed to decode map, is it valid?",
                    "Hitsounder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
                return;
            }

            var silenceProvider = new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)).ToSampleProvider();
            var hitsounds = new List<IWaveProvider>();
            var failed = false;
            foreach (var hitObject in map.HitObjects)
            {
                var hitsound = GetHitsound(hitObject.HitSound, SampleSet.Normal);
                if (hitsound == null)
                {
                    failed = true;
                    break;
                }

                var silence = silenceProvider.Take(hitObject.StartTimeSpan);
                var hitsoundOffset = silence.FollowedBy(hitsound.ToSampleProvider());

                hitsounds.Add(hitsoundOffset.ToWaveProvider());
            }

            if (failed)
                return;

            if (!File.Exists("output")) Directory.CreateDirectory("output");

            var file =
                $"output/{map.MetadataSection.Artist} - {map.MetadataSection.Title} ({map.MetadataSection.Creator}) [{map.MetadataSection.Version}].wav";

            WaveFileWriter.CreateWaveFile16(
                file, 
                new MixingWaveProvider32(hitsounds).ToSampleProvider());

            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", Path.GetFullPath(file)));
        }

        private static void LoadHitsounds(string skinPath)
        {
            var extensions = new List<string> {"ogg", "wav", "mp3"};
            var hitsoundSets = new List<string> {"Normal", "Drum", "Soft"};
            var hitsounds = new List<string> {"Clap", "Finish", "Normal", "Whistle"};

            var shownOggWarning = false;

            foreach (var set in hitsoundSets)
            {
                foreach (var hitsound in hitsounds)
                {
                    foreach (var extension in extensions)
                    {
                        var file = Path.Combine(skinPath, $"{set.ToLower()}-hit{hitsound.ToLower()}.{extension}");
                        if (!File.Exists(file)) continue;

                        if (extension == "ogg" && !shownOggWarning)
                        {
                            MessageBox.Show(
                                "Hitsounder has detected that your skin is using OGG files for one or more hitsounds. " +
                                "Due to certain limitations, Hitsounder can't deal with these types of files correctly and will " +
                                "possibly crash if it cannot convert the file correctly. Please use WAV or MP3 for your hitsounds " +
                                "for guaranteed functionality.",
                                "Hitsounder",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            shownOggWarning = true;
                        }

                        var hitsoundField = typeof(Hitsounds)
                            .GetField($"{set}{hitsound}",
                                BindingFlags.NonPublic | BindingFlags.Static);

                        hitsoundField.SetValue("", file);
                    }
                }
            }
        }

        private static WaveChannel32 GetHitsound(HitSoundType type, SampleSet sampleSet)
        {
            var builder = new StringBuilder();

            // have to do this because sometimes they are none/10 (???)
            switch (sampleSet)
            {
                case SampleSet.Drum:
                    builder.Append("Drum");
                    break;
                case SampleSet.Soft:
                    builder.Append("Soft");
                    break;
                default:
                    builder.Append("Normal");
                    break;
            }

            switch (type)
            {
                case HitSoundType.Clap:
                    builder.Append("Clap");
                    break;
                case HitSoundType.Finish:
                    builder.Append("Finish");
                    break;
                case HitSoundType.Whistle:
                    builder.Append("Whistle");
                    break;
                default:
                    builder.Append("Normal");
                    break;
            }

            var hitsound = typeof(Hitsounds)
                .GetField(builder.ToString(), BindingFlags.NonPublic | BindingFlags.Static);

            var file = hitsound.GetValue("") as string;

            WaveStream reader;
            if (file.EndsWith("ogg"))
            {
                reader = new VorbisWaveReader(file);
            }
            else
            {
                reader = new AudioFileReader(file);
            }

            if (reader.WaveFormat.SampleRate == 44100 && reader.WaveFormat.Channels == 2)
                return new WaveChannel32(reader);

            try
            {
                // have to take steps to convert since I can only do it one at a time which sucks
                var channelConversion = new WaveFormatConversionStream(
                    new WaveFormat(reader.WaveFormat.SampleRate, 2), reader);

                return new WaveChannel32(
                    new WaveFormatConversionStream(
                        new WaveFormat(44100, 2), channelConversion));
            }
            catch (MmException)
            {
                MessageBox.Show(
                    "Failed to convert hitsounds to an applicable format. You may need to install " +
                    "the K-Lite Codec Pack (Basic). If you don't want to do that, just use the default hitsounds.",
                    "Hitsounder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }
    }
}
