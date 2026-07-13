using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FlipnicLib;
using FlipnicLib.Types;
using SukiUI.Dialogs;

namespace FlipnicFileToolGUI.Helpers;

public abstract class RepackUtilsGui
{
    public static async void ReplaceFile(MainWindow mw)
    {
        try
        {
            if (mw.FilesGrid.SelectedItem is not VirtualFile vf) return;
            if (mw.FileName is null) return;
            var replacement = await FileHelpers.OpenFile(mw, [], "Choose a replacement file");
            if (replacement == null) return;
            if (mw.FileName.ToUpper().EndsWith(".ISO"))
            {
                new Thread(() =>
                {
                    new IsoUdf(mw.FileName).ReplaceFile(replacement, mw.FileName, vf.Path);
                    Dispatcher.UIThread.Post(() =>
                    {
                        StaticUtils.LiveLoadStatus = "";
                        mw.ShowDialog("Flipnic file tools", "File replaced successfully.", NotificationType.Success);
                    });
                }).Start();
                StaticUtils.LiveLoadStatus = "Please wait...";
                return;
            }
            var offset = vf.Offset;
            var size = vf.Length;
            var rfi = new FileInfo(replacement);
            var binFiles = new BinFile().GetListBin(File.OpenRead(mw.FileName));
            if (rfi.Length > size)
            {
                var nSize = new FileInfo(replacement).Length;
                var rootDirName = "";
                var rootDirOffset = 0L;
                var rootDirSize = 0L;
                if (vf.LargeBuffer)
                {
                    while ((nSize - vf.Length) % 0x800 != 0)
                    {
                        nSize++;
                    }
                }
                else
                {
                    rootDirName = vf.Path[1..].Split('\\')[0] + "\\";
                    rootDirOffset = binFiles.First(bf => bf.Path == $"\\{rootDirName}").Offset;
                    rootDirSize = binFiles.First(bf => bf.Path == $"\\{rootDirName}").Length;
                }

                await mw.DialogManager.CreateDialog()
                    .WithTitle("CAUTION")
                    .WithContent("It appears the replacement file is bigger than the original file. We will need to update other file records and increase the size of the .BIN file. This should only be done if you know exactly what you're doing. Are you sure you want to continue?")
                    .WithActionButton("Yes", _ =>
                    {
                        StaticUtils.LiveLoadStatus = "Rebuilding .BIN file";
                        new Thread(() =>
                        {
                            if (vf.LargeBuffer)
                            {
                                RepackUtils.ResizeFile(vf.Path, (int)nSize, File.Open(mw.FileName, FileMode.Open), binFiles);
                                RepackUtils.RepackFileUnsafe(offset, File.OpenRead(replacement), mw.FileName, size,
                                    vf.Path[1..].Contains('\\') && !vf.Path[1..].EndsWith('\\') ? 1 : 2048);
                            }
                            else
                            {
                                // Load the entire subfolder to memory
                                var s2 = File.OpenRead(mw.FileName);
                                s2.Seek(rootDirOffset, SeekOrigin.Begin);
                                var ms = new MemoryStream();
                                for (var i = 0; i < rootDirSize; i++)
                                {
                                    ms.WriteByte((byte)s2.ReadByte());
                                }

                                s2.Close();

                                // Resize subfolder entry and overwrite the contents
                                var subF = new Subfolder(ms);
                                var ns = new MemoryStream();
                                var ns1 = subF.ResizeFile(vf.Path.Split('\\')[^1], (int)nSize, ns);
                                var ns2 = subF.WriteFileUnsafe(vf.Path.Split('\\')[^1], File.ReadAllBytes(replacement), ns1);

                                // Ensure that the length can be addressed by 2048 bytes
                                for (var i = 0; i < ns2.Length % 0x800; i++)
                                {
                                    ns2.WriteByte(0);
                                }

                                if (ns2.Length % 0x800 != 0) throw new FormatException("Stream length is not divisible by 2048");
                                ns2.Position = 0;
                                // Resize the subfolder container
                                RepackUtils.ResizeFile(rootDirName, (int)ns2.Length, File.Open(mw.FileName, FileMode.Open), binFiles);
                                RepackUtils.RepackFileUnsafe(rootDirOffset, ns2, mw.FileName, rootDirSize);
                                ns2.Close();
                            }

                            StaticUtils.LiveLoadStatus = "";
                            Dispatcher.UIThread.Post(() =>
                            {
                                mw.ShowDialog("Flipnic file tools", "File replaced successfully.", NotificationType.Success);
                            });
                        }).Start();
                    }, true)
                    .WithActionButton("No", _ =>
                    {
                        new Thread(() =>
                        {
                            Thread.Sleep(200);
                            Dispatcher.UIThread.Post(() => mw.ShowDialog("Flipnic file tools", "No changes were made.", NotificationType.Information));
                        }).Start();
                    }, true)
                    .OfType(NotificationType.Warning)
                    .TryShowAsync();
                return;
            }
            RepackUtils.RepackFileUnsafe(offset, File.OpenRead(replacement), mw.FileName, size, vf.Path[1..].Contains('\\') && !vf.Path[1..].EndsWith('\\') ? 1 : 2048);
            mw.ShowDialog("Flipnic file tools", "File replaced successfully.", NotificationType.Success);
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            mw.ShowDialog("Flipnic file tools", "Operation failed.\n\nError: " + e.Message + "\n" + e.StackTrace, NotificationType.Error);
        }
    }   
}