﻿'    Copyright (C) 2018 Robbie Ward
' 
'    This file is a part of Winapp2ool
' 
'    Winapp2ool is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    Winap2ool is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with Winapp2ool.  If not, see <http://www.gnu.org/licenses/>.
Option Strict On
Imports System.IO
'''    <summary>
'''    This module parses a winapp2.ini file and checks each entry therein 
'''    removing any whose detection parameters do not exist on the current system
'''    and outputting a "trimmed" file containing only entries that exist on the system
'''    to the user.
'''   </summary>
Public Module Trim
    ' File handlers
    Dim winappFile As iniFile = New iniFile(Environment.CurrentDirectory, "winapp2.ini")
    Dim outputFile As iniFile = New iniFile(Environment.CurrentDirectory, "winapp2.ini", "winapp2-trimmed.ini")
    Dim winVer As Double
    ' Module parameters
    Dim detChrome As New List(Of String) _
        From {"%AppData%\ChromePlus\chrome.exe", "%LocalAppData%\Chromium\Application\chrome.exe", "%LocalAppData%\Chromium\chrome.exe", "%LocalAppData%\Flock\Application\flock.exe", "%LocalAppData%\Google\Chrome SxS\Application\chrome.exe",
                           "%LocalAppData%\Google\Chrome\Application\chrome.exe", "%LocalAppData%\RockMelt\Application\rockmelt.exe", "%LocalAppData%\SRWare Iron\iron.exe", "%ProgramFiles%\Chromium\Application\chrome.exe", "%ProgramFiles%\SRWare Iron\iron.exe",
                           "%ProgramFiles%\Chromium\chrome.exe", "%ProgramFiles%\Flock\Application\flock.exe", "%ProgramFiles%\Google\Chrome SxS\Application\chrome.exe", "%ProgramFiles%\Google\Chrome\Application\chrome.exe", "%ProgramFiles%\RockMelt\Application\rockmelt.exe",
                           "HKCU\Software\Chromium", "HKCU\Software\SuperBird", "HKCU\Software\Torch", "HKCU\Software\Vivaldi"}
    Dim settingsChanged As Boolean
    Dim download As Boolean = False
    Dim downloadNCC As Boolean = False
    Dim areDownloading As Boolean = False

    ''' <summary>
    ''' Restores the default state of the module's parameters
    ''' </summary>
    Private Sub initDefaultSettings()
        settingsChanged = False
        winappFile.resetParams()
        outputFile.resetParams()
        download = False
        downloadNCC = False
    End Sub

    ''' <summary>
    ''' Initializes the default module settings and returns references to them to the calling function
    ''' </summary>
    ''' <param name="firstFile">The winapp2.ini file</param>
    ''' <param name="secondFile">The output file</param>
    ''' <param name="d">Download boolean</param>
    ''' <param name="dncc">Non-CCleaner download boolean</param>
    Public Sub initTrimParams(ByRef firstFile As iniFile, ByRef secondFile As iniFile, ByRef d As Boolean, ByRef dncc As Boolean)
        initDefaultSettings()
        firstFile = winappFile
        secondFile = outputFile
        d = download
        dncc = downloadNCC
    End Sub

    ''' <summary>
    ''' Runs the trimmer from outside the module
    ''' </summary>
    ''' <param name="firstFile">The winapp2.ini file</param>
    ''' <param name="secondFile">The output file</param>
    ''' <param name="d">Download boolean</param>
    ''' <param name="ncc">Non-CCleaner download boolean</param>
    Public Sub remoteTrim(firstFile As iniFile, secondFile As iniFile, d As Boolean, ncc As Boolean)
        winappFile = firstFile
        outputFile = secondFile
        download = d
        downloadNCC = ncc
        initTrim()
    End Sub

    ''' <summary>
    ''' Prints the main menu to the user
    ''' </summary>
    Public Sub printMenu()
        printMenuTop({"Trim winapp2.ini such that it contains only entries relevant to your machine,", "greatly reducing both application load time and the winapp2.ini file size."})
        print(1, "Run (default)", "Trim winapp2.ini")
        print(1, "Toggle Download", $"{enStr(download)} using the latest winapp2.ini as the input file", Not isOffline, True)
        print(1, "Toggle Download (Non-CCleaner)", $"{enStr(downloadNCC)} using the latest Non-CCleaner winapp2.ini as the input file", download, trailingBlank:=True)
        print(1, "File Chooser (winapp2.ini)", "Change the winapp2.ini name or location", Not (download Or downloadNCC), isOffline, True)
        print(1, "File Chooser (save)", "Change the save file name or location", trailingBlank:=True)
        print(0, $"Current winapp2.ini location: {If(download, GetNameFromDL(download, downloadNCC), replDir(winappFile.path))}")
        print(0, $"Current save location: {replDir(outputFile.path)}", closeMenu:=Not settingsChanged)
        print(2, "Trim", cond:=settingsChanged, closeMenu:=True)
    End Sub

    ''' <summary>
    ''' Handles the user input from the menu
    ''' </summary>
    ''' <param name="input">The String containing the user's input</param>
    Public Sub handleUserInput(input As String)
        Select Case True
            Case input = "0"
                exitModule("Trim")
            Case (input = "1" Or input = "")
                initTrim()
            Case input = "2" And Not isOffline
                toggleDownload(download, settingsChanged)
                If downloadNCC And Not download Then downloadNCC = False
            Case input = "3" And download
                toggleDownload(downloadNCC, settingsChanged)
            Case (input = "3" And Not (download Or downloadNCC) And Not isOffline) Or (input = "2" And isOffline)
                changeFileParams(winappFile, settingsChanged)
            Case (input = "4" And Not isOffline) Or (input = "3" And isOffline)
                changeFileParams(outputFile, settingsChanged)
            Case (input = "5" Or (input = "4" And isOffline)) And settingsChanged
                resetModuleSettings("Trim", AddressOf initDefaultSettings)
            Case Else
                menuHeaderText = invInpStr
        End Select
    End Sub

    ''' <summary>
    ''' Initiates the trim after validating our ini files
    ''' </summary>
    Private Sub initTrim()
        If Not download Then winappFile.validate()
        If pendingExit() Then Exit Sub
        Dim winapp2 As winapp2file = If(Not download, New winapp2file(winappFile), New winapp2file(getRemoteIniFile(If(downloadNCC, nonccLink, wa2Link))))
        trim(winapp2)
        menuHeaderText = "Trim Complete"
        Console.Clear()
    End Sub

    ''' <summary>
    ''' Performs the trim
    ''' </summary>
    ''' <param name="winapp2">A winapp2.ini file</param>
    Private Sub trim(winapp2 As winapp2file)
        Console.Clear()
        print(0, tmenu("Trimming... Please wait, this may take a moment..."), closeMenu:=True)
        Dim entryCountBeforeTrim As Integer = winapp2.count
        For Each entryList In winapp2.winapp2entries
            processEntryList(entryList)
        Next
        winapp2.rebuildToIniFiles()
        winapp2.sortInneriniFiles()
        print(0, tmenu("Finished!"), closeMenu:=True)
        Console.Clear()
        print(0, tmenu("Trim Complete"))
        print(0, menuStr03)
        print(0, "Entry Count", isCentered:=True, trailingBlank:=True)
        print(0, $"Initial: {entryCountBeforeTrim}")
        print(0, $"Trimmed: {winapp2.count}")
        Dim difference As Integer = entryCountBeforeTrim - winapp2.count
        print(0, $"{difference} entries trimmed from winapp2.ini ({Math.Round((difference / entryCountBeforeTrim) * 100)}%)")
        print(0, anyKeyStr, leadingBlank:=True, closeMenu:=True)
        outputFile.overwriteToFile(winapp2.winapp2string)
        If Not suppressOutput Then Console.ReadKey()
    End Sub

    ''' <summary>
    ''' Evaluates a list of keys to observe whether they exist on the current machine
    ''' </summary>
    ''' <param name="keyList">The list of iniKeys to query</param>
    ''' <param name="chkExist">The function that evaluates that keyType's parameters</param>
    ''' <returns></returns>
    Private Function checkExistence(ByRef keyList As List(Of iniKey), chkExist As Func(Of String, Boolean)) As Boolean
        If keyList.Count = 0 Then Return False
        For Each key In keyList
            If chkExist(key.value) Then Return True
        Next
        Return False
    End Function

    ''' <summary>
    ''' Returns true if an entry's detection criteria is matched by the system, false otherwise.
    ''' </summary>
    ''' <param name="entry">A winapp2.ini entry</param>
    ''' <returns></returns>
    Private Function processEntryExistence(ByRef entry As winapp2entry) As Boolean
        Dim hasDetOS As Boolean = Not entry.detectOS.Count = 0
        Dim hasMetDetOS As Boolean = False
        ' Process the DetectOS if we have one, take note if we meet the criteria, otherwise return false
        If hasDetOS Then
            If winVer = Nothing Then winVer = getWinVer()
            hasMetDetOS = checkExistence(entry.detectOS, AddressOf checkDetOS)
            If Not hasMetDetOS Then Return False
        End If
        ' Process any other Detect criteria we have
        If checkExistence(entry.specialDetect, AddressOf checkSpecialDetects) Then Return True
        If checkExistence(entry.detects, AddressOf checkRegExist) Then Return True
        If checkExistence(entry.detectFiles, AddressOf checkFileExist) Then Return True
        ' Return true for the case where we have only a DetectOS and we meet its criteria 
        If hasMetDetOS And entry.specialDetect.Count = 0 And entry.detectFiles.Count = 0 And entry.detects.Count = 0 Then Return True
        ' Return true for the case where we have no valid detect criteria
        If entry.detectOS.Count + entry.detectFiles.Count + entry.detects.Count + entry.specialDetect.Count = 0 Then Return True
        Return False
    End Function

    ''' <summary>
    ''' Processess a list of winapp2.ini entries and removes any from the list that wouldn't be detected by CCleaner
    ''' </summary>
    ''' <param name="entryList">The list of winapp2entry objects to check existence for</param>
    Private Sub processEntryList(ByRef entryList As List(Of winapp2entry))
        Dim sectionsToBePruned As New List(Of winapp2entry)
        For Each entry In entryList
            If Not processEntryExistence(entry) Then sectionsToBePruned.Add(entry)
        Next
        removeEntries(entryList, sectionsToBePruned)
    End Sub

    ''' <summary>
    ''' Returns true if a SpecialDetect location exist, false otherwise
    ''' </summary>
    ''' <param name="key">A SpecialDetect format iniKey</param>
    ''' <returns></returns>
    Private Function checkSpecialDetects(ByVal key As String) As Boolean
        Select Case key
            Case "DET_CHROME"
                For Each path As String In detChrome
                    If checkExist(path) Then Return True
                Next
            Case "DET_MOZILLA"
                Return checkFileExist("%AppData%\Mozilla\Firefox")
            Case "DET_THUNDERBIRD"
                Return checkFileExist("%AppData%\Thunderbird")
            Case "DET_OPERA"
                Return checkFileExist("%AppData%\Opera Software")
        End Select
        ' If we didn't return above, SpecialDetect definitely doesn't exist
        Return False
    End Function

    ''' <summary>
    ''' Handles passing off checks for the DET_CHROME case
    ''' </summary>
    ''' <param name="key">An individual Detect parameter for the DET_CHROME case</param>
    ''' <returns></returns>
    Private Function checkExist(key As String) As Boolean
        Return If(key.StartsWith("HK"), checkRegExist(key), checkFileExist(key))
    End Function

    ''' <summary>
    ''' Returns True if a given Detect path exists in the system registry, false otherwise.
    ''' </summary>
    ''' <param name="path">A registry path from a Detect key</param>
    ''' <returns></returns>
    Private Function checkRegExist(path As String) As Boolean
        Dim dir As String = path
        Dim root As String = getFirstDir(path)
        Try
            Select Case root
                Case "HKCU"
                    dir = dir.Replace("HKCU\", "")
                    Return Microsoft.Win32.Registry.CurrentUser.OpenSubKey(dir, True) IsNot Nothing
                Case "HKLM"
                    dir = dir.Replace("HKLM\", "")
                    If Microsoft.Win32.Registry.LocalMachine.OpenSubKey(dir, True) IsNot Nothing Then Return True
                    ' Small workaround for newer x64 versions of Windows
                    dir = dir.ToLower.Replace("software\", "Software\WOW6432Node\")
                    Return Microsoft.Win32.Registry.LocalMachine.OpenSubKey(dir, True) IsNot Nothing
                Case "HKU"
                    dir = dir.Replace("HKU\", "")
                    Return Microsoft.Win32.Registry.Users.OpenSubKey(dir, True) IsNot Nothing
                Case "HKCR"
                    dir = dir.Replace("HKCR\", "")
                    Return Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(dir, True) IsNot Nothing
            End Select
        Catch ex As Exception
            ' The most common (only?) exception here is a permissions one, so assume true if we hit because a permissions exception implies the key exists anyway.
            Return True
        End Try
        ' If we didn't return anything above, registry location probably doesn't exist
        Return False
    End Function

    ''' <summary>
    ''' Returns True if a DetectFile path exists on the system, false otherwise.
    ''' </summary>
    ''' <param name="key">A DetectFile path</param>
    ''' <returns></returns>
    Private Function checkFileExist(key As String) As Boolean
        Dim isProgramFiles As Boolean = False
        Dim dir As String = key
        ' Make sure we get the proper path for environment variables
        If dir.Contains("%") Then
            Dim splitDir As String() = dir.Split(CChar("%"))
            Dim var As String = splitDir(1)
            Dim envDir As String = Environment.GetEnvironmentVariable(var)
            Select Case var
                Case "ProgramFiles"
                    isProgramFiles = True
                Case "Documents"
                    envDir = $"{Environment.GetEnvironmentVariable("UserProfile")}\{If(winVer = 5.1, "My ", "")}Documents"
                Case "CommonAppData"
                    envDir = Environment.GetEnvironmentVariable("ProgramData")
            End Select
            dir = envDir + splitDir(2)
        End If
        Try
            ' Process wildcards appropriately if we have them 
            If dir.Contains("*") Then
                Dim exists As Boolean = False
                exists = expandWildcard(dir)
                ' Small contingency for the isProgramFiles case
                If Not exists And isProgramFiles Then
                    swapDir(dir, key)
                    exists = expandWildcard(dir)
                End If
                Return exists
            End If
            ' Check out those file/folder paths
            If Directory.Exists(dir) Or File.Exists(dir) Then Return True
            ' If we didn't find it and we're looking in Program Files, check the (x86) directory
            If isProgramFiles Then
                swapDir(dir, key)
                Return (Directory.Exists(dir) Or File.Exists(dir))
            End If
        Catch ex As Exception
            exc(ex)
            Return True
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Swaps out a directory with the ProgramFiles parameterization on 64bit computers
    ''' </summary>
    ''' <param name="dir">The text to be modified</param>
    ''' <param name="key">The original state of the text</param>
    Private Sub swapDir(ByRef dir As String, key As String)
        Dim envDir As String = Environment.GetEnvironmentVariable("ProgramFiles(x86)")
        dir = envDir & key.Split(CChar("%"))(2)
    End Sub

    ''' <summary>
    ''' Interpret parameterized wildcards for the current system
    ''' </summary>
    ''' <param name="dir">A path containing a wildcard</param>
    ''' <returns></returns>
    Private Function expandWildcard(dir As String) As Boolean
        ' This should handle wildcards anywhere in a path even though CCleaner only supports them at the end for DetectFiles
        Dim possibleDirs As New List(Of String)
        Dim currentPaths As New List(Of String)
        currentPaths.Add("")
        ' Split the given string into sections by directory 
        Dim splitDir As String() = dir.Split(CChar("\"))
        For Each pathPart In splitDir
            ' If this directory parameterization includes a wildcard, expand it appropriately
            ' This probably wont work if a string for some reason starts with a *
            If pathPart.Contains("*") Then
                For Each currentPath In currentPaths
                    Try
                        ' Query the existence of child paths for each current path we hold 
                        Dim possibilities As String() = Directory.GetDirectories(currentPath, pathPart)
                        ' If there are any, add them to our possibility list
                        If Not possibilities.Count = 0 Then possibleDirs.AddRange(possibilities)
                    Catch
                        ' Pretty much exception we'll encounter here is going to be the result of directories not existing.
                        ' The exception will be thrown from the GetDirectories call and will prevent us from attempting to add new
                        ' items to the possibility list. In this instance, we can silently fail (here). 
                    End Try
                Next
                ' If no possibilities remain, the wildcard parameterization hasn't left us with any real paths on the system, so we may return false.
                If possibleDirs.Count = 0 Then Return False
                ' Otherwise, clear the current paths and repopulate them with the possible paths 
                currentPaths.Clear()
                currentPaths.AddRange(possibleDirs)
                possibleDirs.Clear()
            Else
                If currentPaths.Count = 0 Then
                    currentPaths.Add($"{pathPart}\")
                Else
                    Dim newCurPaths As New List(Of String)
                    For Each path As String In currentPaths
                        If Directory.Exists($"{path}{pathPart}\") Then newCurPaths.Add($"{path}{pathPart}\")
                    Next
                    currentPaths = newCurPaths
                End If
            End If
        Next
        ' If any file/path exists, return true
        For Each currDir In currentPaths
            If Directory.Exists(currDir) Or File.Exists(currDir) Then Return True
        Next
        Return False
    End Function

    ''' <summary>
    ''' Returns true if we satisfy the DetectOS citeria, false otherwise
    ''' </summary>
    ''' <param name="value">The DetectOS criteria to be checked</param>
    ''' <returns></returns>
    Private Function checkDetOS(value As String) As Boolean
        Dim splitKey As String() = value.Split(CChar("|"))
        Return If(value.StartsWith("|"), Not winVer > Double.Parse(splitKey(1)), Not winVer < Double.Parse(splitKey(0)))
    End Function
End Module