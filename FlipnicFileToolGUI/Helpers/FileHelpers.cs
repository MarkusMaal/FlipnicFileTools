using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BigGustave;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib;
using FlipnicLib.Formats;
using FlipnicLib.Formats.Jam;
using FlipnicLib.Formats.Midi;
using FlipnicLib.Formats.Vag;
using FlipnicLib.Types;
using SukiUI.Controls;
using Syroot.BinaryData;

namespace FlipnicFileToolGUI.Helpers;

public static class FileHelpers
{
    /// <summary>
    /// Displays an open file dialog
    /// </summary>
    /// <param name="form">Form or control that invoked this method</param>
    /// <param name="filters">File type filters</param>
    /// <param name="title">Window title for the file picker</param>
    /// <returns>Fully decoded absolute path to the file</returns>
    public static async Task<string?> OpenFile(Control form, IReadOnlyList<FilePickerFileType>? filters, string title = "Open file")
    {
        if (Design.IsDesignMode) return null;
        var topLevel = TopLevel.GetTopLevel(form);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        return files.Count <= 0 ? null : Uri.UnescapeDataString(files[0].Path.AbsolutePath);
    }

    /// <summary>
    /// Displays a save file dialog
    /// </summary>
    /// <param name="form">Form or control that invoked this method</param>
    /// <param name="filters">File type filters</param>
    /// <param name="title">Window title for the file picker</param>
    /// <returns>Fully decoded absolute path to the file</returns>
    public static async Task<string?> SaveFile(Control form, IReadOnlyList<FilePickerFileType>? filters, string title = "Save file")
    {
        if (Design.IsDesignMode) return null;
        var topLevel = TopLevel.GetTopLevel(form);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            FileTypeChoices = filters
        });

        return file != null ? Uri.UnescapeDataString(file.Path.AbsolutePath) : null;
    }

    /// <summary>
    /// Displays a folder picker dialog
    /// </summary>
    /// <param name="form">Form or control that invoked this method</param>
    /// <param name="title">Window title for the folder picker</param>
    /// <returns>Fully decoded path to the folder</returns>
    public static async Task<string?> SelectFolder(Control form, string title = "Select folder")
    {
        var topLevel = TopLevel.GetTopLevel(form);
        var storageFiles = await topLevel!.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = title
            });
        return storageFiles.Count == 0 ? null : Uri.UnescapeDataString(storageFiles[0].Path.AbsolutePath);
    }

    /// <summary>
    /// Displays the ToString() output of a class object inside the info box
    /// </summary>
    /// <param name="sender">Object we want to get the ToString() output from</param>
    /// <param name="type">File type description displayed at the top right</param>
    /// <param name="mw">Main window instance</param>
    private static void LoadAsString(object? sender, string type, MainWindow mw)
    {
        Dispatcher.UIThread.Post(() =>
        {
            mw.InfoBox.WrapText = false;
            mw.InfoBox.Text = sender?.ToString() ?? "";
            mw.InfoTab.IsVisible = true;
            mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, type);
        });
    }
    
    /// <summary>
    /// Tries to parse file data from the stream provided
    /// </summary>
    /// <param name="ds">Stream to decode data from</param>
    /// <param name="ext">File extension (without .)</param>
    /// <param name="mw">Main window instance</param>
    public static void LoadFromData(Stream ds, string? ext, MainWindow mw)
    {
        mw.FileTypeLabel.Content = "Please wait...";
        foreach (var t in mw.MainTabControl.Items)
        {
            ((SukiSideMenuItem)t!).IsVisible = false;
        }

        mw.DockPanel1.IsVisible = false;
        mw.Loader.IsVisible = true;
        mw.AdsrPanel.IsVisible = false;
        mw.WavToggle.IsVisible = false;
        mw.FakeSustainRateToggle.IsVisible = false;

        List<VirtualFile> fsEntries;

        new Thread(() =>
        {
            switch (ext?.ToUpper())
            {
                case "TM2":
                    StaticUtils.LiveLoadStatus = "Parsing TIM2";
                    var data = new byte[ds.Length];
                    ds.ReadExactly(data);
                    ds.Position = 0;
                    var img = new Tim2(data, mw.FileName!);
                    var bt = new BitmapTools { Image = img };
                    var finalBmp = bt.ToBitmap();
                    LoadAsString(img, "PlayStation 2 texture file", mw);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.ImagePreviewTab.IsVisible = true;
                        mw.PreviewImage.Source = finalBmp;
                    });
                    break;
                case "ICO":
                    data = new byte[ds.Length];
                    ds.ReadExactly(data);
                    ds.Position = 0;
                    var ico = new SaveIcon(data);
                    ico.Read();
                    bt = new BitmapTools { Icon = ico.Texture };


                    StaticUtils.LiveLoadStatus = "Initializing OpenGL";
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.ModelTab.IsSelected = true;
                        mw.InfoTab.IsSelected = false;
                    });
                    Thread.Sleep(1000);
                    LoadAsString(ico, "PlayStation 2 save file icon", mw);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.GlControl.ImportICO(ico);
                        mw.Init3DStuff();
                        mw.ModelTab.IsSelected = false;
                        mw.ModelTab.IsVisible = true;
                        mw.ImagePreviewTab.IsVisible = true;
                        mw.PreviewImage.Source = bt.IconToBitmap();
                    });
                    break;
                case "MID":
                    if (!File.Exists(mw.FileName))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            mw.InfoBox.Text = "You must extract this file before opening it";
                            mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "General MIDI");
                            mw.InfoTab.IsVisible = true;
                        });
                        break;
                    }
                    var midi = new Midi(mw.FileName);
                    midi.Read(ds);
                    LoadAsString(midi, "General MIDI", mw);
                    break;
                case "FPD":
                    var fpd = new FpnFpd(ds);

                    StaticUtils.LiveLoadStatus = "Initializing OpenGL";
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.ModelTab.IsSelected = true;
                        mw.InfoTab.IsSelected = false;
                    });
                    
                    Thread.Sleep(1000);
                    LoadAsString(fpd, "Fixed Path Data", mw);
                    Dispatcher.UIThread.Post(() =>
                    {
                        var texture = StaticUtils.GenerateCheckerboardPng(16, 32,
                            new Pixel(64, 51, 102, 180, false), new Pixel(0, 255, 0, 255, false));
                        mw.GlControl.ImportFPD(fpd, texture);
                        mw.Init3DStuff();
                        if (Debugger.IsAttached)
                        {
                            mw.ImagePreviewTab.IsVisible = true;
                            mw.PreviewImage.Source = new Bitmap(texture);
                        }

                        mw.ModelTab.IsSelected = false;
                        mw.ModelTab.IsVisible = true;
                    });
                    break;
                case "VSD":
                    var vsd = new FpnVsd(ds);
                    LoadAsString(vsd, "Vibration Strength Data", mw);
                    break;
                case "BD":
                case ".BD":
                    Dispatcher.UIThread.Post(() => mw.GetViewModel().Samples = []);
                    var s = new Samples(ds);
                    var samples = new List<SampleColl>();
                    var offset = 0;
                    for (var i = 0; i < s.RawSamples.Count; i++)
                    {
                        samples.Add(new SampleColl
                        {
                            Data = s.RawSamples[i],
                            Id = i,
                            Offset = offset + 0x10,
                            LoopStart = s.LoopStarts[i],
                            LoopEnd = s.LoopEnds[i],
                        });
                        offset += s.Lengths[i];
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.GetViewModel().Samples = new ObservableCollection<SampleColl>(samples);
                        mw.BdSampleTab.IsVisible = true;
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "JAM body");
                    });
                    break;
                case "HD":
                case ".HD":
                    var jh = new JamHeader();
                    try
                    {
                        jh.Read(new BinaryStream(ds));
                    }
                    catch (InvalidDataException)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            mw.ShowDialog("Flipnic File Tools",
                                "Cannot parse this file, because it's missing the SShd header. The file may be corrupt or incompatible with this program.",
                                NotificationType.Error);
                            mw.InfoTab.IsVisible = true;
                            mw.InfoBox.Text = "Error opening file!";
                            mw.FileTypeLabel.Content = "Ready";
                            mw.Title = "Flipnic file tool";
                        });
                        break;
                    }

                    LoadAsString(jh, "JAM header", mw);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.ConvertTab.IsVisible = jh.ProgramChunks.Count > 0;
                        mw.FfmpegBrowserGrid.IsVisible = false;
                        mw.BdBrowserGrid.IsVisible = true;
                        mw.MidiBrowserGrid.IsVisible = true;
                        mw.PalToggle.IsVisible = false;
                        mw.EnvelopeToggle.IsVisible = true;
                        mw.ConvertSf2Button.IsVisible = true;
                        mw.ConvertMovAacButton.IsVisible = false;
                        mw.ConvertMovButton.IsVisible = false;
                        mw.DemuxButton.IsVisible = false;
                        mw.AdsrPanel.IsVisible = true;
                        mw.WavToggle.IsVisible = true;
                        mw.FakeSustainRateToggle.IsVisible = true;

                        var fileDirectory = new FileInfo(mw.FileName!).Directory?.FullName ?? "";
                        var extension = Path.GetExtension(mw.FileName);
                        var fileName = new FileInfo(mw.FileName!).Name.Replace(extension!, "");
                        var bdPath = Path.Combine(fileDirectory, fileName) + ".BD";
                        var midPath = Path.Combine(fileDirectory, fileName) + ".MID";
                        if (File.Exists(bdPath)) mw.BdBox.Text = bdPath;
                        if (File.Exists(midPath)) mw.MidiBox.Text = midPath;
                    });
                    break;
                case "CSV":
                case "TXT":
                case "XML":
                case "CNF":
                    var txt = Encoding.UTF8.GetString(ds.ReadBytes((int)ds.Length));
                    LoadAsString(txt, ext switch
                    {
                        "CNF" => "PlayStation title information",
                        "CSV" => "Comma Separated Values",
                        "XML" => "eXtensible Markup Language",
                        _ => "Plain Text"
                    }, mw);
                    break;
                case "SVAG":
                case "INT":
                case "VAG":
                    var va = new byte[ds.Length];
                    ds.ReadExactly(va);
                    SonyVag.Decode(va);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.SoundPlayerTab.IsVisible = true;
                        mw.AudioFilename.Content = "Filename: " + Path.GetFileName(mw.FileName);
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat,
                            "Compressed Sony ADPCM Audio " + (mw.FileName!.EndsWith("INT") ? "(Stereo)" : "(Mono)"));
                    });
                    break;
                case "MLB":
                    var mlbDa = new byte[ds.Length];
                    ds.ReadExactly(mlbDa);
                    var mlb = new FpnMlb(mlbDa);
                    StaticUtils.LiveLoadStatus = "Generating menu...";
                    Dispatcher.UIThread.Post(() => mw.GetViewModel().Menu.Clear());
                    var menuIndex = 0;
                    Dispatcher.UIThread.Post(() => mw.LoadProgress.IsIndeterminate = false);
                    Dispatcher.UIThread.Post(() => mw.LoadProgress.Maximum = mlb.Sections.Count);
                    var mbCheckerboard = new Bitmap(StaticUtils.GenerateCheckerboardPng(128, 128));
                    foreach (var (key, value) in mlb.Sections)
                    {
                        try
                        {
                            var mevm = new List<MenuElementViewModel>();
                            foreach (var ima in value)
                            {
                                StaticUtils.LiveLoadStatus = "Parsing " + ima.Texture;
                                var p = Path.Combine(Path.GetDirectoryName(mw.FileName) ?? string.Empty,
                                    ima.Texture.Split('\\')[^1].ToUpper());
                                Tim2? tim2 = null;
                                if (File.Exists(p))
                                {
                                    tim2 = new Tim2(File.ReadAllBytes(p), mw.FileName!);
                                    foreach (var check in mlb.MenuColors)
                                    {
                                        if ((key == check.SectionLabel) && (check.Index == ima.Index))
                                        {
                                            tim2.ReplaceColor(check.Color);
                                        }
                                    }
                                }
                                var bmp = File.Exists(p)
                                    ? new BitmapTools
                                        { Image = tim2, }.ToBitmap()
                                    : mbCheckerboard;
                                Dispatcher.UIThread.Post(() => mevm.Add(new MenuElementViewModel(){Layer = key, MenuElement = ima, ImageSource = bmp}));
                            }
                            Dispatcher.UIThread.Post(() => mw.GetViewModel().Menu.AddRange(mevm));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("ERROR: " + ex.Message);
                        }

                        Dispatcher.UIThread.Post(() => mw.LoadProgress.Value = ++menuIndex);
                    }

                    Dispatcher.UIThread.Post(() => mw.LoadProgress.IsIndeterminate = true);
                    StaticUtils.LiveLoadStatus = "Please wait...";


                    Dispatcher.UIThread.Post(() =>
                    {
                        var idx = -32768;
                        var orderedMenus = new List<MenuElementViewModel>();
                        while (true)
                        {
                            var idx1 = idx;
                            var layer = mw.GetViewModel().Menu.Where(iter => iter.MenuElement.Dipth == idx1);
                            if (idx == 32768) break;
                            orderedMenus.AddRange(layer);
                            idx++;
                        }

                        mw.MenuMockupTab.IsVisible = true;
                        mw.MenuMockup.MenuElementSource =
                            new ObservableCollection<MenuElementViewModel>(orderedMenus);
                    });
                    LoadAsString(mlb, "Menu layout file", mw);
                    break;
                case "LP4":
                    var lp4Da = new byte[ds.Length];
                    ds.ReadExactly(lp4Da);
                    var lp4 = new Lp4(lp4Da, mw.FileName!);
                    if (lp4.HasEmbeddedResources)
                    {
                        StaticUtils.LiveLoadStatus = "Initializing OpenGL";
                        Dispatcher.UIThread.Post(() =>
                        {
                            mw.ModelTab.IsSelected = true;
                            mw.InfoTab.IsSelected = false;
                        });
                        if (Program.GpuAccel) Thread.Sleep(1000);
                        StaticUtils.LiveLoadStatus = "Parsing LP4";
                        mw.GlControl.ImportLP4(lp4);
                    }
                    LoadAsString(lp4, "Flipnic resource file", mw);
                    if (!lp4.HasEmbeddedResources) break;
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.Init3DStuff(lp4);
                        mw.ModelTab.IsSelected = false;
                        mw.ModelTab.IsVisible = true;
                        mw.ImagePreviewTab.IsVisible = true;
                        try
                        {
                            var ms = new MemoryStream(lp4.Texture);
                            mw.PreviewImage.Source = new Bitmap(ms);
                        }
                        catch
                        {
                            mw.ImagePreviewTab.IsVisible = false;
                        }

                        if (!mw.GlControl.IsTextureValid())
                        {
                            mw.ImagePreviewTab.IsVisible = false;
                        }
                    });
                    break;
                case "IPU":
                    var ipu = Ipu.GetInfoAsString(ds);
                    LoadAsString(ipu, "IPU video stream", mw);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.ConvertTab.IsVisible = true;
                        mw.ConvertMovButton.IsVisible = true;
                        mw.ConvertMovAacButton.IsVisible = false;
                        mw.FfmpegBrowserGrid.IsVisible = true;
                        mw.PalToggle.IsVisible = true;
                        mw.EnvelopeToggle.IsVisible = false;
                        mw.ConvertSf2Button.IsVisible = false;
                        mw.DemuxButton.IsVisible = false;
                        mw.BdBrowserGrid.IsVisible = false;
                        mw.MidiBrowserGrid.IsVisible = false;
                    });
                    break;
                case "COL":
                    var col = new FpnCol(ds);
                    LoadAsString(col, "Collision map", mw);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.ColTab.IsVisible = true;
                        mw.ColMap.ColObject = col; 
                    });
                    break;
                case "LIT":
                    var lit = new FpnLit(ds);
                    LoadAsString(lit, "Light map", mw);
                    break;
                case "SCC":
                    var das = new byte[ds.Length];
                    ds.ReadExactly(das);
                    var vss = new VssVer(das);
                    LoadAsString(vss, "Source code control file", mw);
                    break;
                case "FTL":
                    var ftl = new FpnTexList(ds);
                    LoadAsString(ftl, "Texture list", mw);
                    break;
                case "LAY":
                    var da = new byte[ds.Length];
                    ds.ReadExactly(da);
                    var lay = new FpnLay(da);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.LayTab.IsVisible = true;
                        mw.StageLayoutsControl.LayoutSource = new ObservableCollection<FpnLay.Layout>(lay.Layouts);
                        mw.FileTypeLabel.Content = "Stage layout file";
                    });
                    break;
                case "MSG":
                    var msg = new FpnMsg(ds);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.MsgEditor.MsgObject = msg;
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "Message table");
                        mw.MessageEditorTab.IsVisible = true;
                        mw.MessageEditorTab.IsSelected = true;
                    });
                    break;
                case "FPC":
                    var fpc = new FpnFpc(ds);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.CameraTool.CameraObject = fpc;
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "Camera sequence");
                        mw.CameraToolTab.IsVisible = true;
                        mw.CameraToolTab.IsSelected = true;
                    });
                    break;
                case "SST":
                    var sst = new FpnSst(ds);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.InfoBox.Text = $"Entries\n{sst.ListEntries()}\n\nCameras:\n{sst.GetCamData()}\n\nResources\n{sst.GenerateMagicNumbers()}";
                        mw.GetViewModel().Gimmicks = sst.GetGimmicks();
                        mw.StageGimmickTab.IsVisible = mw.GetViewModel().Gimmicks?.Count > 0;
                        mw.GimmickCombobox.Items.Clear();
                        foreach (var key in mw.GetViewModel().Gimmicks?.Keys.ToArray() ?? [])
                        {
                            mw.GimmickCombobox.Items.Add(key);
                        }

                        mw.PseudoCodeTab.IsVisible = sst.TableOfContents.ContainsKey("EVENT");
                        if (mw.PseudoCodeTab.IsVisible)
                        {
                            mw.EventBox.Text = sst.GeneratePseudoCode();
                        }

                        if (sst.HasScoreRecord())
                        {
                            mw.GetViewModel().SaveData = sst.GetSaveFromRecord();
                            mw.SaveEditor.IsVisible = true;
                        }

                        mw.GimmickCombobox.SelectedIndex = 0;
                        mw.InfoTab.IsVisible = true;
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "Stage information file");
                        if (mw.PseudoCodeTab.IsVisible && StaticUtils.MsgFile == "")
                        {
                            mw.ShowDialog("Flipnic file tools", "JA.MSG not loaded. Event pseudo-code will show numbers instead of actual mission names. To fix this, select \"Import JA.MSG\" from the options menu and then reload the .SST file.", NotificationType.Warning);
                        }
                    });
                    break;
                case "BIN":
                    mw.Fs = new BinFile();
                    mw.Fs.FsEntries.Clear();
                    mw.Fs.ListBin(ds);

                    fsEntries = mw.Fs.FsEntries.ToList();
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.GetViewModel().VirtualFiles = new ObservableCollection<VirtualFile>(fsEntries);
                        mw.FileListTab.IsVisible = true;
                        mw.FilesGrid.ItemsSource = mw.GetViewModel().VirtualFiles;
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "Blob file");
                        mw.OpenButton.IsVisible = true;
                        mw.ExtractButton.IsVisible = true;
                    });
                    break;
                case "ISO":
                    ds.Close(); // fix access violation
                    mw.IsoFile = new IsoUdf(mw.FileName!);
                    fsEntries = mw.IsoFile.GetFiles().ToList();
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.GetViewModel().VirtualFiles = new ObservableCollection<VirtualFile>(fsEntries);
                        mw.FileListTab.IsVisible = true;
                        mw.FilesGrid.ItemsSource = mw.GetViewModel().VirtualFiles;
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "UDF disc image");
                        mw.OpenButton.IsVisible = false;
                        mw.ExtractButton.IsVisible = false;
                    });
                    break;
                case "PSS":
                    var pssInfo = new Pss(mw.FileName!).ListPss(ds);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.InfoBox.Text = pssInfo;
                        mw.ConvertTab.IsVisible = true;
                        mw.InfoTab.IsVisible = true;
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "Interleaved video/audio streams");
                        mw.ConvertMovButton.IsVisible = false;
                        mw.ConvertMovAacButton.IsVisible = true;
                        mw.DemuxButton.IsVisible = true;
                        mw.FfmpegBrowserGrid.IsVisible = true;
                        mw.PalToggle.IsVisible = true;
                        mw.EnvelopeToggle.IsVisible = false;
                        mw.ConvertSf2Button.IsVisible = false;
                        mw.BdBrowserGrid.IsVisible = false;
                        mw.MidiBrowserGrid.IsVisible = false;
                    });
                    break;
                case ".49":
                case ".57":
                case ".65":
                case ".50":
                case "49":
                case "57":
                case "65":
                case "50":
                    var game = new Game(ds);
                    LoadAsString(game, "Game Executable", mw);
                    break;
                case "DAT":
                    LoadAsString(new Dummy(ds), "Dummy file", mw);
                    break;
                default:
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.InfoBox.Text = "Unrecognized file type";
                        mw.FileTypeLabel.Content = string.Format(MainWindow.FTypeFormat, "Unknown");
                        mw.InfoTab.IsVisible = true;
                    });
                    break;
            }

            ds.Close();

            StaticUtils.LiveLoadStatus = "";
            Dispatcher.UIThread.Post(() =>
            {
                mw.DockPanel1.IsVisible = true;
                mw.Loader.IsVisible = false;
                // switch to first visible tab
                mw.MainTabControl.UnselectAll();
                foreach (SukiSideMenuItem? sSmi in mw.MainTabControl.Items)
                {
                    if (sSmi is not { IsVisible: true }) continue;
                    sSmi.IsSelected = true;
                    break;
                }
            });
        }).Start();
        // display loading screen if applicable
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "")
                {
                    Dispatcher.UIThread.Post(() => mw.LoadProgress.IsIndeterminate = true);
                    break;
                }
                Dispatcher.UIThread.Post(() =>
                {
                    mw.LoadProgress.IsIndeterminate = !(StaticUtils.LiveLoadStatus?.Contains('%') ?? false);
                    if (!mw.LoadProgress.IsIndeterminate)
                    {
                        mw.LoadProgress.Maximum = 100;
                        try
                        {
                            mw.LoadProgress.Value =
                                int.Parse(StaticUtils.LiveLoadStatus!.Split(" (")[1].Split('%')[0].Split('.')[0]);
                        }
                        catch
                        {
                            mw.LoadProgress.Value = 0;
                        }
                    }
                    mw.LoadStatus.Text = StaticUtils.LiveLoadStatus;
                });
                Thread.Sleep(100);
            }
        }).Start();
    }
    
    /// <summary>
    /// Checks if the file is locked
    /// </summary>
    /// <param name="file">The file to check</param>
    /// <returns>True when the file is locked</returns>
    public static bool IsFileLocked(FileInfo file)
    {
        try
        {
            using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
        }
        catch (IOException)
        {
            return true;
        }
        return false;
    }
}