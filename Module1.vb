Imports System.Configuration.Install
Imports System.ComponentModel
Imports System.ServiceProcess
Imports System.IO
Imports System.Threading
Public Class IndexMonitorService
   Inherits System.ServiceProcess.ServiceBase
   Private DirToMonitor As String
   Private DirsToSync As List(Of String)
   Private indexWatcher As FileSystemWatcher
   Private components As System.ComponentModel.IContainer
   Public Sub New()
      MyBase.New()
      InitializeComponent()
      Me.CanStop = True
      Me.CanPauseAndContinue = True
      Me.CanShutdown = True
   End Sub
   Protected Overrides Sub OnStart(ByVal args() As String)
      If BadConfiguration() Then
         Me.Stop()
         Exit Sub
      End If
      indexWatcher = New FileSystemWatcher
      indexWatcher.Path = DirToMonitor
      indexWatcher.NotifyFilter = (NotifyFilters.FileName Or NotifyFilters.LastWrite Or NotifyFilters.CreationTime Or NotifyFilters.DirectoryName)
      indexWatcher.Filter = "finished"
      indexWatcher.InternalBufferSize = 131072
      indexWatcher.IncludeSubdirectories = True
      AddHandler indexWatcher.Created, AddressOf OnCreated
      indexWatcher.EnableRaisingEvents = True
      LogThis("Index Monitor Started.")
   End Sub
   Protected Overrides Sub OnStop()
      LogThis("Index Monitor Stopped.")
   End Sub
   Protected Overrides Sub OnPause()
      indexWatcher.EnableRaisingEvents = False
      LogThis("Index Monitor Paused.")
   End Sub
   Protected Overrides Sub OnContinue()
      indexWatcher.EnableRaisingEvents = True
      LogThis("Index Monitor Continued.")
   End Sub
   <System.Diagnostics.DebuggerNonUserCode()> _
   Protected Overrides Sub Dispose(ByVal disposing As Boolean)
      Try
         If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
         End If
      Finally
         MyBase.Dispose(disposing)
      End Try
   End Sub
   <MTAThread()> <System.Diagnostics.DebuggerNonUserCode()> _
  Shared Sub Main()
      Dim ServicesToRun() As System.ServiceProcess.ServiceBase
      ServicesToRun = New System.ServiceProcess.ServiceBase() {New IndexMonitorService}
      System.ServiceProcess.ServiceBase.Run(ServicesToRun)
   End Sub
   <System.Diagnostics.DebuggerStepThrough()> _
   Private Sub InitializeComponent()
      Me.ServiceName = "IndexMonitorService"
   End Sub
   Private Sub RunMonitor()
      Dim watcher As New FileSystemWatcher()
      watcher.Path = DirToMonitor
      watcher.NotifyFilter = (NotifyFilters.FileName Or NotifyFilters.LastWrite Or NotifyFilters.CreationTime Or NotifyFilters.DirectoryName)
      watcher.Filter = "finished"
      watcher.InternalBufferSize = 131072
      watcher.IncludeSubdirectories = True
      AddHandler watcher.Created, AddressOf OnCreated
      watcher.EnableRaisingEvents = True
      'While started
      'End While
   End Sub
   Private Sub OnCreated(ByVal source As Object, ByVal e As FileSystemEventArgs)
      If File.Exists(e.FullPath) Then
         LogThis("CREATED: " & e.FullPath)
         If NewIndexOnlyHere(e.FullPath) Then
            Dim parentFolder As DirectoryInfo = Directory.GetParent(e.FullPath)
            Dim aMove As New CopyFolder(parentFolder, DirsToSync, DirToMonitor)
            Dim moveThread As New Thread(AddressOf aMove.Copy)
            moveThread.Start()
            LogThis("QUEUED : " & parentFolder.FullName & " for move.")
         End If
      End If
   End Sub
   Private Function NewIndexOnlyHere(ByVal anIndex As String) As Boolean
      For Each dirToSyc In DirsToSync
         If File.Exists(anIndex.Replace(DirToMonitor, dirToSyc)) Then
            Return False
         End If
      Next
      Return True
   End Function
   Function BadConfiguration() As Boolean
      Dim ConfigFile As String = My.Application.Info.DirectoryPath & "\" & My.Application.Info.ProductName & ".conf"
      If My.Computer.FileSystem.FileExists(ConfigFile) Then
         LogThis("Found Configuration file: " & ConfigFile)
      Else
         LogThis("Configuration file not found at: " & ConfigFile)
         LogThis("Progam will not continue.")
         Return True
      End If
      DirsToSync = New List(Of String)
      Dim sr As New StreamReader(ConfigFile)
      Try
         Do
            Dim s As String = sr.ReadLine
            Dim ss() As String = s.Split(">")
            Select Case ss(0)
               Case "DirToMonitor"
                  DirToMonitor = ss(1).Trim
                  If My.Computer.FileSystem.DirectoryExists(DirToMonitor) Then
                     LogThis("Found Directory to Monitor: " & DirToMonitor)
                  Else
                     LogThis("Did not find Directory to Monitor: " & DirToMonitor)
                     LogThis("Progam will not continue.")
                     sr.Close()
                     Return True
                  End If
               Case "DirToSync1"
                  Dim Dir1 As String = ss(1).Trim
                  If Directory.Exists(Dir1) Then
                     DirsToSync.Add(Dir1)
                     LogThis("Found Directory to Synchronize 1: " & Dir1)
                  Else
                     LogThis("Did not find Directory to Synchronize 1: " & Dir1)
                  End If
               Case "DirToSync2"
                  Dim Dir2 As String = ss(1).Trim
                  If Directory.Exists(Dir2) Then
                     DirsToSync.Add(Dir2)
                     LogThis("Found Directory to Synchronize 2: " & Dir2)
                  Else
                     LogThis("Did not find Directory to Synchronize 2: " & Dir2)
                  End If
               Case "DirToSync3"
                  Dim Dir3 As String = ss(1).Trim
                  If Directory.Exists(Dir3) Then
                     DirsToSync.Add(Dir3)
                     LogThis("Found Directory to Synchronize 2: " & Dir3)
                  Else
                     LogThis("Did not find Directory to Synchronize 2: " & Dir3)
                  End If
               Case "DirToSync4"
                  Dim Dir4 As String = ss(1).Trim
                  If Directory.Exists(Dir4) Then
                     DirsToSync.Add(Dir4)
                     LogThis("Found Directory to Synchronize 2: " & Dir4)
                  Else
                     LogThis("Did not find Directory to Synchronize 2: " & Dir4)
                  End If
            End Select
         Loop Until sr.Peek = -1
      Catch ex As Exception
         LogThis(ConfigFile & " is empty.  Program will not continue.")
         Return True
      Finally
         sr.Close()
      End Try
      If String.IsNullOrEmpty(DirToMonitor) Then
         LogThis("Directory to Monitor directive not configured.  Program will not continue.")
         Return True
      End If
      If DirsToSync.Count < 1 Then
         LogThis("None of the Synchronization directories were found.")
         LogThis("Progam will not continue.")
         Return True
      End If
      Return False
   End Function
   Private Sub LogThis(ByVal info As String)
      Dim logfile As String = My.Application.Info.DirectoryPath & "\" & My.Application.Info.ProductName & ".log"
      Dim timestamp As String = "[" & Now.ToString("yyyy-MM-dd HH:mm:ss") & "] "
      My.Computer.FileSystem.WriteAllText(logfile, timestamp & info & vbCrLf, True)
   End Sub
End Class
<RunInstaller(True)> _
Public Class IndexMonitorServiceInstaller
   Inherits Installer
   Private myServiceInstaller As ServiceInstaller
   Private myServiceProcessInstaller As ServiceProcessInstaller
   Public Sub New()
      MyBase.New()
      myServiceInstaller = New ServiceInstaller
      myServiceProcessInstaller = New ServiceProcessInstaller
      myServiceProcessInstaller.Account = ServiceAccount.NetworkService
      myServiceInstaller.StartType = ServiceStartMode.Manual
      myServiceInstaller.ServiceName = "Roknet Index Monitor"
      myServiceInstaller.Description = "Service that monitors and replicates Lucene index changes to other Roknet web servers"
      Installers.Add(myServiceInstaller)
      Installers.Add(myServiceProcessInstaller)
   End Sub
   Public Overrides Sub Install(ByVal savedState As IDictionary)
      MyBase.Install(savedState)
      Console.WriteLine("On Installer, Install")
   End Sub
   Public Overrides Sub Commit(ByVal savedState As IDictionary)
      MyBase.Commit(savedState)
      Console.WriteLine("On Installer, Commit")
   End Sub
   Public Overrides Sub Rollback(ByVal savedState As IDictionary)
      MyBase.Rollback(savedState)
   End Sub
   Public Overrides Sub Uninstall(ByVal savedState As IDictionary)
      MyBase.Uninstall(savedState)
   End Sub
End Class
Public Class CopyFolder
   Private folderToCopy As DirectoryInfo
   Private DirsToSync As List(Of String)
   Private DirToMonitor As String
   Public Sub New(ByVal aFolder As DirectoryInfo, ByVal FoldersToSych As List(Of String), ByVal FolderToMonitor As String)
      folderToCopy = aFolder
      DirsToSync = FoldersToSych
      DirToMonitor = FolderToMonitor
   End Sub
   Public Sub Copy()
      Thread.Sleep(5000)
        If Directory.Exists(folderToCopy.FullName) Then
            For Each DirToSyc As String In DirsToSync
                Dim newDir As String = folderToCopy.FullName.Replace(DirToMonitor, DirToSyc)
                Try
                    Directory.CreateDirectory(newDir)
                Catch ex As Exception
                    LogThis("Error creating directory " & newDir & ": " & ex.Message)
                End Try
                LogThis("Copying: " & folderToCopy.FullName & " to " & newDir & "...")
                For Each oneFile As FileInfo In folderToCopy.GetFiles
                    Try
                        Dim newFile As New FileInfo(oneFile.FullName.Replace(DirToMonitor, DirToSyc))
                        If oneFile.Exists Then
                            oneFile.CopyTo(newFile.FullName, True)
                        End If
                    Catch ex As Exception
                        LogThis("Error copying file " & oneFile.FullName & " to " & newDir & ": " & ex.Message)
                    End Try
                Next
                LogThis("COPIED : " & newDir)
            Next
        End If
        RemoveExtraFolders()
   End Sub
   Private Sub RemoveExtraFolders()
      Dim baseFolder As New DirectoryInfo(folderToCopy.FullName.Replace(folderToCopy.Name, ""))
      For Each oneDir As String In DirsToSync
         Dim aSycDir As New DirectoryInfo(baseFolder.FullName.Replace(DirToMonitor, oneDir))
         For Each subDir As DirectoryInfo In aSycDir.GetDirectories
            If Not Directory.Exists(subDir.FullName.Replace(oneDir, DirToMonitor)) Then
               Try
                  LogThis("Deleting extra directory: " & subDir.FullName)
                  If subDir.Exists Then
                     subDir.Delete(True)
                  End If
               Catch ex As Exception
                  'LogThis("Error deleting extra directory: " & subDir.FullName & ": " & ex.Message)
               End Try
            End If
         Next
      Next
   End Sub
   Private Sub LogThis(ByVal info As String)
      Dim logfile As String = My.Application.Info.DirectoryPath & "\" & My.Application.Info.ProductName & ".log"
      Dim timestamp As String = "[" & Now.ToString("yyyy-MM-dd HH:mm:ss") & "] "
      My.Computer.FileSystem.WriteAllText(logfile, timestamp & info & vbCrLf, True)
   End Sub
End Class