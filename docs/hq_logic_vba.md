--- Macro File: Sheet3.cls ---
Attribute VB_Name = "Sheet3"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "Label2, 105, 1, MSForms, Label"
Attribute VB_Control = "Label1, 104, 2, MSForms, Label"
Attribute VB_Control = "CommandButton9, 2, 3, MSForms, CommandButton"
Attribute VB_Control = "CommandButton12, 3, 4, MSForms, CommandButton"
Attribute VB_Control = "CommandButton10, 4, 5, MSForms, CommandButton"
Attribute VB_Control = "OptionButton1, 5, 6, MSForms, OptionButton"
Attribute VB_Control = "OptionButton2, 6, 7, MSForms, OptionButton"
Attribute VB_Control = "OptionButton3, 7, 8, MSForms, OptionButton"
Attribute VB_Control = "OptionButton4, 8, 9, MSForms, OptionButton"
Attribute VB_Control = "OptionButton5, 9, 10, MSForms, OptionButton"
Attribute VB_Control = "OptionButton6, 10, 11, MSForms, OptionButton"
Attribute VB_Control = "OptionButton7, 11, 12, MSForms, OptionButton"
Attribute VB_Control = "OptionButton8, 12, 13, MSForms, OptionButton"
Attribute VB_Control = "OptionButton9, 13, 14, MSForms, OptionButton"
Attribute VB_Control = "OptionButton10, 14, 15, MSForms, OptionButton"
Attribute VB_Control = "CommandButton1, 24, 16, MSForms, CommandButton"
Attribute VB_Control = "CommandButton2, 27, 17, MSForms, CommandButton"
Attribute VB_Control = "CommandButton3, 28, 18, MSForms, CommandButton"
Attribute VB_Control = "CommandButton6, 63, 19, MSForms, CommandButton"
Attribute VB_Control = "Label3, 106, 20, MSForms, Label"
Attribute VB_Control = "Label4, 107, 21, MSForms, Label"
Attribute VB_Control = "CommandButton7, 108, 22, MSForms, CommandButton"
Attribute VB_Control = "CommandButton8, 109, 23, MSForms, CommandButton"
Attribute VB_Control = "CommandButton11, 110, 24, MSForms, CommandButton"
Attribute VB_Control = "CommandButton13, 111, 25, MSForms, CommandButton"
Attribute VB_Control = "CommandButton4, 32, 26, MSForms, CommandButton"
Attribute VB_Control = "CommandButton5, 34, 27, MSForms, CommandButton"
Attribute VB_Control = "CRCExport, 141, 28, MSForms, CommandButton"
Attribute VB_Control = "ExtraDeals, 143, 29, MSForms, ToggleButton"
Attribute VB_Control = "OptionButton11, 145, 30, MSForms, OptionButton"
Attribute VB_Control = "OptionButton12, 146, 31, MSForms, OptionButton"
Attribute VB_Control = "OptionButton13, 147, 32, MSForms, OptionButton"
Attribute VB_Control = "OptionButton14, 148, 33, MSForms, OptionButton"
Attribute VB_Control = "OptionButton15, 149, 34, MSForms, OptionButton"
'Sub to establish repository connection
Private Sub CommandButton11_Click()
On Error Resume Next

'Dialog in case of an already established connection
If Worksheets("Index").Range("Path_Repository").value <> "" Then
    intSure = MsgBox("Connection is already stored. Do you want to replace?", vbYesNo, "Replace existing connection?")
    If intSure = 7 Then
        Exit Sub
    End If
End If

Dim rep_path As String

'Selection of repository via User Dialog
rep_path = Application.GetOpenFilename(fileFilter:="Access files (*.mdb), *.mdb")

'Check if mdb-file contains table "Deal_Storage, if not selected file is not a usable repository
If rep_path <> False Then
    
    Dim cnt As New ADODB.Connection
    Dim rst As New ADODB.Recordset
    Dim cmd As ADODB.Command
    Dim glob_sConnect As String
    
    glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
    With cnt
      .Provider = "Microsoft.Jet.OLEDB.4.0"
      .Properties("Jet OLEDB:Database Password") = pw_DB
      .Mode = adModeRead
      .Open glob_sConnect
    End With
    Set cmd = New ADODB.Command
    cmd.CommandText = "SELECT * from Deal_Storage"
    cmd.ActiveConnection = ADOC
    Set DBS = cmd.Execute
    
    Err.Clear
    
    If Err.Number <> 0 Then
        MsgBox "Connection to Repository could not be established. Maybe you selected the wrong correct Repository"
        Err.Clear
        Exit Sub
    End If
    Worksheets("Index").Range("Path_Repository").value = rep_path
    MsgBox "Connection to Repository was successfully established"
End If

End Sub

'Sub to store all deals within the portfolio sheet in the repository
Private Sub CommandButton13_Click()
'On Error GoTo Fehler

Dim i As Integer
Dim j As Integer
Dim PK As Integer
Dim intSure As Integer
Dim intColumn As Integer
Dim dealnr As Integer
Dim First As Integer
Dim last As Integer
Dim Deals_vorhanden As Boolean
Dim Max As Integer


'Init Database Connection

Dim ADOC As New ADODB.Connection
Dim DBS As New ADODB.Recordset
Dim s2 As ADODB.Command
Dim glob_sConnect As String
    
glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
With ADOC
    .Provider = "Microsoft.Jet.OLEDB.4.0"
    .Properties("Jet OLEDB:Database Password") = pw_DB
    .Mode = adModeReadWrite
    .Open glob_sConnect
End With

With DBS
 .CursorType = adOpenKeyset
 .LockType = adLockOptimistic
End With

Set s2 = New ADODB.Command

'Check if connection to a usable repository is established
If Worksheets("Index").Range("Path_Repository").value = "" Then
    MsgBox "No Repository connection established!"
    Exit Sub
End If

Deals_vorhanden = False

'Check if portfolio sheet contains at least one deal to store
For i = 10 To 52 Step 3
    If Worksheets("Portfolio").Cells(11, i) <> "" Then
        Deals_vorhanden = True
    End If
Next

'Message if no deal to store is available
If Deals_vorhanden = False Then
    MsgBox ("No Deals within portfolio")
    Exit Sub
End If

intSure = MsgBox(" Do you want to export the portfolio to repository", vbYesNo, "Store Deal in Repository")
If intSure = 7 Then
    Exit Sub
End If

With Application
            .Calculate
            .Calculation = xlManual
            .MaxChange = 0.001
            .ScreenUpdating = False
End With

ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Unprotect Password:="Blattschutz"

'"Select" to gather currently highest deal_id that is used to derive Deal_IDs for the new deals
s2.CommandText = "SELECT [Deal_ID] From [Deal_Storage] order by [Deal_ID]"
s2.ActiveConnection = ADOC

Set DBS = s2.Execute
'If the repository already contains datasets then the highest deal_id is derived
'else pk is set to 9999 that pk +1 results in 10000 as the deal_id for the first deal
If Not DBS.EOF Then
    DBS.MoveFirst
Else
    'Database is empty, so the first ID will be 10000
    PK = 9999
End If
    
Do While Not DBS.EOF
   PK = DBS.Fields(0).value
   DBS.MoveNext
Loop
DBS.Close

'Exception for US to automatically generate an Quote_ID, for all other Countries field "D4" on portfolio sheet is used for Quote_ID
'Automatically generated Quote_IDs are stored in a separate table within repository
If [Country_Short] = "USA" Then
    s2.CommandText = "SELECT [Quote_ID] From [Quote_NR] order by [Quote_ID]"
    Set DBS = s2.Execute
    If Not DBS.EOF Then
        DBS.MoveFirst
    Else
        'Database is empty, so the first ID will be 10000
        Max = 9999
    End If
    
    Do While Not DBS.EOF
        Max = DBS.Fields(0).value
        DBS.MoveNext
    Loop
    DBS.Close
    DBS.Open "[Quote_NR]", ADOC, adOpenKeyset, adLockOptimistic
    DBS.AddNew
    DBS.Fields(0).value = Max + 1
    DBS.Update
    DBS.Close
End If

'Exception for France where beside standard fields also fields from local sheet are stored within separate Repository table
First = PK + 1

DBS.Open "[Deal_Storage]", ADOC, adOpenKeyset, adLockOptimistic

'Inserting of all non-dealer deals from portfolio sheet in repository

'upto 10 deals
If Worksheets("Portfolio").Range("BC10").value = 0 Then
    For intColumn = 10 To 37 Step 3
        If Worksheets("Portfolio").Cells(11, intColumn) <> "" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business (Detailed)" Then
            DBS.AddNew
            DBS.Fields(0).value = PK + 1
            'In case of US, automatically generated quote_ID's are used instead of field "D4" on portfolio sheet
            If [Country_Short] = "USA" Then
                DBS.Fields(1).value = Max + 1
            Else
                DBS.Fields(1).value = Worksheets("Portfolio").Range("D4").value
            End If
            For j = 2 To 119
                DBS.Fields(j).value = Worksheets("Portfolio").Cells(7 + j, intColumn).value
            Next
            DBS.Fields(120).value = Format(Now(), "Short Date")
            DBS.Fields(121).value = Worksheets("Portfolio").Range("D2").value
            DBS.Fields(122).value = Worksheets("Portfolio").Range("D6").value
            DBS.Fields(123).value = Worksheets("Portfolio").Range("F35").value
            DBS.Update
            PK = PK + 1
        End If
    Next

Else

'upto 15 deals
    For intColumn = 10 To 52 Step 3
        If Worksheets("Portfolio").Cells(11, intColumn) <> "" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business (Detailed)" Then
            DBS.AddNew
            DBS.Fields(0).value = PK + 1
            'In case of US, automatically generated quote_ID's are used instead of field "D4" on portfolio sheet
            If [Country_Short] = "USA" Then
                DBS.Fields(1).value = Max + 1
            Else
                DBS.Fields(1).value = Worksheets("Portfolio").Range("D4").value
            End If
            For j = 2 To 119
                DBS.Fields(j).value = Worksheets("Portfolio").Cells(7 + j, intColumn).value
            Next
            DBS.Fields(120).value = Format(Now(), "Short Date")
            DBS.Fields(121).value = Worksheets("Portfolio").Range("D2").value
            DBS.Fields(122).value = Worksheets("Portfolio").Range("D6").value
            DBS.Fields(123).value = Worksheets("Portfolio").Range("F35").value
            DBS.Update
            PK = PK + 1
        End If
    Next
 
End If

DBS.Close

'Storage of "local"-fields in separate repository table
'Mapping of Deals to the local fields is ensured by using the same Deal_IDs

If [Country_Short] = "FRA" And Worksheets("Portfolio").Columns("AN:BA").Hidden = True Then
    PK = First - 1
    DBS.Open "[Local]", ADOC, adOpenKeyset, adLockOptimistic
    For intColumn = 10 To 31 Step 3
        If Worksheets("Portfolio").Cells(11, intColumn) <> "" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business (Detailed)" Then
            PK = PK + 1
            DBS.AddNew
            DBS.Fields(0).value = PK
            For j = 1 To 20
                DBS.Fields(j).value = Worksheets("Portfolio").Cells(142 + j, intColumn).value
            Next
            DBS.Update
        End If
    Next
    DBS.Close
End If


If [Country_Short] = "FRA" And Worksheets("Portfolio").Columns("AN:BA").Hidden = False Then
    PK = First - 1
    DBS.Open "[Local]", ADOC, adOpenKeyset, adLockOptimistic
    For intColumn = 10 To 52 Step 3
        If Worksheets("Portfolio").Cells(11, intColumn) <> "" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business" And Worksheets("Portfolio").Cells(11, intColumn) <> "Dealer Retail Business (Detailed)" Then
            PK = PK + 1
            DBS.AddNew
            DBS.Fields(0).value = PK
            For j = 1 To 20
                DBS.Fields(j).value = Worksheets("Portfolio").Cells(142 + j, intColumn).value
            Next
            DBS.Update
        End If
    Next
    DBS.Close
End If

ADOC.Close

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
last = PK

'Message with Deal_id's of stored deals and also quote_id's in case of USA
If [Country_Short] = "USA" Then
    MsgBox "Portfolio was successfully stored in repository with unique Repository number " & First & " until " & last & " and Quote ID " & Max + 1 & "."
Else
    MsgBox "Portfolio was successfully stored in repository with unique Repository number " & First & " until " & last & "."
End If

Exit Sub

'Error message in case of abort
Fehler:
If Err.Number = 3024 Then
    MsgBox "Database was not found. Please check repository connection.", 0, "An error occured."
Else
    MsgBox "An error occured. Deals could not be stored"
End If

'closing of recordset conncetion if still open in case of abort
If DBS Is Nothing Then
Else
    DBS.Close
End If

'closing of database conncetion if still open in case of abort
If ADOC Is Nothing Then
Else
    ADOC.Close
End If

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"

End Sub

Private Sub CommandButton15_Click()

End Sub

'Sub to export portfolio in a separate sheet
'To store the current portfolio in a separate file the values from the portfolio sheet are copied into 2 temp sheets
'portfolio2 contains only the visible main information, portfolio 3 also the invisible information below the visible main information
Private Sub CommandButton4_Click()
On Error GoTo Fehler

With Application
        .Calculate
        .Calculation = xlManual
        .MaxChange = 0.001
        .ScreenUpdating = False
End With
ActiveWorkbook.PrecisionAsDisplayed = False

Worksheets("Portfolio").Unprotect Password:="Blattschutz"

tmpName = ActiveWorkbook.Name
Worksheets("Portfolio3").Unprotect Password:="Blattschutz"
Worksheets("Portfolio2").Unprotect Password:="Blattschutz"
Worksheets("Portfolio3").Visible = True
Worksheets("Portfolio2").Visible = True
Worksheets("Portfolio2").Activate

With ActiveSheet.PageSetup
    .CenterHeader = "DFS RORAC Tool Version " & [APPVERSION]
End With


Dim eins As String
Dim zwei As String

'Copying of main visible information to portfolio2
Worksheets("Portfolio2").Range("A1:AZ37").Select
Selection.Clear
Worksheets("Portfolio").Activate

With ActiveSheet.PageSetup
    .CenterHeader = "DFS RORAC Tool Version " & [APPVERSION]
End With

If Worksheets("Portfolio").Range("BC10") = 1 Then
    Worksheets("Portfolio").Range("A1:AZ37").Select
Else
    Worksheets("Portfolio").Range("A1:AL37").Select
End If
Selection.Copy

With Worksheets("Portfolio2")
    .Activate
    .Range("A1").Select
    .Range("A1").PasteSpecial Paste:=xlValues, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
    .Range("A1").PasteSpecial Paste:=xlFormats, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
    .Range("A1").PasteSpecial Paste:=xlPasteComments, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
    If [Anz_Dea_Rea] > 0 Then
        .Columns("H:H").EntireColumn.Hidden = False
    Else
        .Columns("H:H").EntireColumn.Hidden = True
    End If
End With

'Copying of main visible and invisible information to portfolio3
Worksheets("Portfolio3").Activate

With ActiveSheet.PageSetup
    .CenterHeader = "DFS RORAC Tool Version " & [APPVERSION]
End With

Worksheets("Portfolio3").Range("A1:AL128").Select
Selection.Clear
Worksheets("Portfolio").Activate
Worksheets("Portfolio").Range("A1:AL128").Select
Selection.Copy
With Worksheets("Portfolio3")
    .Activate
    .Range("A1").Select
    .Range("A1").PasteSpecial Paste:=xlValues, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
    .Range("A1").PasteSpecial Paste:=xlFormats, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
    .Range("A1").PasteSpecial Paste:=xlPasteComments, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
    .Range("D38:Al137").Font.Color = black
    If [Anz_Dea_Rea] > 0 Then
        .Columns("H:H").EntireColumn.Hidden = False
    Else
        .Columns("H:H").EntireColumn.Hidden = True
    End If
End With

'Copy sheets into a new file with "Copy"-command
Sheets(Array("Portfolio2", "Portfolio3")).Select
Sheets("Portfolio2").Activate
Sheets(Array("Portfolio2", "Portfolio3")).Copy
Worksheets("Portfolio3").Protect Password:="Blattschutz"
'Hide Portfolio3 in export file with all information
Worksheets("Portfolio3").Visible = False
Worksheets("Portfolio2").Protect Password:="Blattschutz"
'Copy colors from tool to export file
ActiveWorkbook.Colors = Workbooks(tmpName).Colors
'ActiveSheet.Shapes.SelectAll
'Selection.Delete
ActiveSheet.Range("D2").Select
Application.CutCopyMode = False
'Save new File
fname = Application.GetSaveAsFilename(fileFilter:="Excel files (*.xlsx), *.xlsx")
Application.DisplayAlerts = False
If fname <> False Then
ActiveWorkbook.SaveAs Filename:= _
        fname, FileFormat:=xlOpenXMLWorkbook _
        , Password:="", WriteResPassword:="", ReadOnlyRecommended:=False, _
        CreateBackup:=False
End If
Application.DisplayAlerts = True
ActiveWorkbook.Saved = True
ActiveWorkbook.Close 'savechanges:=False
'Hide and protect temp files in tool
Worksheets("Portfolio2").Visible = False
Worksheets("Portfolio3").Visible = False
Worksheets("Portfolio3").Protect Password:="Blattschutz"
Worksheets("Portfolio2").Protect Password:="Blattschutz"
Worksheets("Portfolio").Activate
Worksheets("Portfolio").Range("A1").Select

'Protects Sheet after finishing
Application.Calculate
With Application
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
Exit Sub

'In case of error temp files will be hidden and protected and procedure will be aborted
Fehler:
MsgBox "An Error occured. Maybe you are not allowed to save the file in the selected folder."
tmpName2 = ActiveWorkbook.Name

If tmpName <> tmpName2 Then
    ActiveWorkbook.Saved = True
    ActiveWorkbook.Close
End If

Workbooks(tmpName).Activate
Worksheets("Portfolio2").Visible = False
Worksheets("Portfolio3").Visible = False
Worksheets("Portfolio3").Protect Password:="Blattschutz"
Worksheets("Portfolio2").Protect Password:="Blattschutz"
Worksheets("Portfolio").Activate
Worksheets("Portfolio").Range("A1").Select

'Protects Sheet after finishing

With Application
    .Calculate
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With

ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
End Sub

'Sub to open a form for adding dealer retail business
'Before form is opened some checks are executed
Private Sub CommandButton5_Click()

Dim clm As Integer
Dim wksPortfolio As Worksheet
Dim wksIndex As Worksheet
Dim wksinput As Worksheet


Set wksPortfolio = Sheets("Portfolio")
Set wksinput = Sheets("New Input Mask")
Set wksIndex = Sheets("Index")

clm = 10

'Check if already on dealer retail business was added due to the point that just one can be added
If [Anz_Dea_Rea] = 1 Then
    MsgBox ("Dealer Retail Business was already added. Please delete existing one first before you input new information")
    Exit Sub
End If

If Worksheets("Portfolio").Range("BC10").value = 1 Then
    'Check if 10 deals are already added to the portfolio so that there is no space anymore
    Do While wksPortfolio.Cells(11, clm).value <> ""
        clm = clm + 3
        If clm > 52 Then
            MsgBox ("Too many Deals in Portfolio, please delete one or consolidate with another deal")
            Exit Sub
        End If
    Loop
    Else
    
    Do While wksPortfolio.Cells(11, clm).value <> ""
        clm = clm + 3
        If clm > 37 Then
            MsgBox ("Too many Deals in Portfolio, please delete one or consolidate with another deal")
            Exit Sub
        End If
    Loop
End If




'Open form for Dealer Retail Business
Frm_Dea_Ret.Show
End Sub

'Special button for US and Mexico to open the Sheet "Local_Output"
Private Sub CommandButton6_Click()
Worksheets("Local_Output").Select
Worksheets("Local_Output").[b6].Activate
Application.EnableEvents = True
End Sub

'Sub to open a form to import deals from repository
Private Sub CommandButton7_Click()
If Worksheets("Index").Range("Path_Repository").value = "" Then
    MsgBox "No Repository connection established"
    Exit Sub
Else
    Frm_Cen_Rep.Show
End If
End Sub

'Sub to store selected deal in repository
Private Sub CommandButton8_Click()
On Error GoTo Fehler

Dim i As Integer
Dim j As Integer
Dim PK As Integer
Dim intSure As Integer
Dim intColumn As Integer
Dim dealnr As Integer
Dim ADOC As New ADODB.Connection
Dim DBS As New ADODB.Recordset
Dim cmd As ADODB.Command
Dim glob_sConnect As String
Dim NumberOfDealsInRep As Integer
            
glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
With ADOC
    .Provider = "Microsoft.Jet.OLEDB.4.0"
    .Properties("Jet OLEDB:Database Password") = pw_DB
    .Mode = adModeReadWrite
    .Open glob_sConnect
End With

With DBS
 .CursorType = adOpenKeyset
 .LockType = adLockOptimistic
End With

Set cmd = New ADODB.Command

Dim Max As Integer

If Worksheets("Index").Range("Path_Repository").value = "" Then
    MsgBox "No Repository connection established"
    Exit Sub
End If

'Check which deal is selected and shall be exported
If OptionButton1 = True Then intColumn = 10
If OptionButton2 = True Then intColumn = 13
If OptionButton3 = True Then intColumn = 16
If OptionButton4 = True Then intColumn = 19
If OptionButton5 = True Then intColumn = 22
If OptionButton6 = True Then intColumn = 25
If OptionButton7 = True Then intColumn = 28
If OptionButton8 = True Then intColumn = 31
If OptionButton9 = True Then intColumn = 34
If OptionButton10 = True Then intColumn = 37
If OptionButton11 = True Then intColumn = 40
If OptionButton12 = True Then intColumn = 43
If OptionButton13 = True Then intColumn = 46
If OptionButton14 = True Then intColumn = 49
If OptionButton15 = True Then intColumn = 52

dealnr = (intColumn - 7) / 3

'Check if selected deal contains data
If Worksheets("Portfolio").Cells(11, intColumn) = "" Then
    MsgBox ("No Data for Deal " & CStr(dealnr) & " available")
    Exit Sub
End If

'Check if selected deal is not Dealer Retail Business
If Worksheets("Portfolio").Cells(11, intColumn) = "Dealer Retail Business" Or Worksheets("Portfolio").Cells(11, intColumn) = "Dealer Retail Business (Detailed)" Then
    MsgBox ("Dealer Retail Business cannot be exported to Repository")
    Exit Sub
End If

intSure = MsgBox(" Do you want to store deal " & CStr(dealnr) & " in repository?", vbYesNo, "Store Deal in Repository" & Str(dealnr))
If intSure = 7 Then
    Exit Sub
End If

With Application
        .Calculate
        .Calculation = xlManual
        .MaxChange = 0.001
        .ScreenUpdating = False
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Unprotect Password:="Blattschutz"

'"Select" to identify highest deal-id and derive deal_id for export deal
Set cmd = New ADODB.Command
   
cmd.CommandText = "SELECT [Deal_ID] From [Deal_Storage] order by [Deal_ID]"
cmd.ActiveConnection = ADOC

Set DBS = cmd.Execute
NumberOfDealsInRep = 0

' Here we check, which is the last deal in repository to get the next ID for it
If Not DBS.EOF Then
    DBS.MoveFirst
Else
    'Database is empty, so the first ID will be 10000
    PK = 9999
End If
    
Do While Not DBS.EOF
   PK = DBS.Fields(0).value
   DBS.MoveNext
Loop

DBS.Close

'Exception for US: Also the quote_id will be generated automatically with the table "Quote_NR"
If [Country_Short] = "USA" Then
    cmd.CommandText = "SELECT [Quote_ID] From [Quote_NR] order by [Quote_ID]"
    cmd.ActiveConnection = ADOC
    Set DBS = cmd.Execute
    If Not DBS.EOF Then
        DBS.MoveFirst
    Else
        Max = 9999
    End If
    
    Do While Not DBS.EOF
        Max = DBS.Fields(0).value
        DBS.MoveNext
    Loop
    'close old socket to create a new one and insert new entry
    DBS.Close
    
    DBS.Open "[Quote_NR]", ADOC, adOpenKeyset, adLockOptimistic
    DBS.AddNew
    DBS.Fields(0).value = Max + 1
    DBS.Update
    DBS.Close
End If

'Export of deal information and insert into repository table
DBS.Open "[Deal_Storage]", ADOC, adOpenKeyset, adLockOptimistic
DBS.AddNew
DBS.Fields(0).value = PK + 1
If [Country_Short] = "USA" Then
    DBS.Fields(1).value = Max + 1
Else
    DBS.Fields(1).value = Worksheets("Portfolio").Range("D4").value
End If
For j = 2 To 119
    DBS.Fields(j).value = Worksheets("Portfolio").Cells(7 + j, intColumn).value
Next
DBS.Fields(120).value = Format(Now(), "Short Date")
DBS.Fields(121).value = Worksheets("Portfolio").Range("D2").value
DBS.Fields(122).value = Worksheets("Portfolio").Range("D6").value
DBS.Update
DBS.Close

'Storage of additional information from local sheet for France. Information will be stored in a extra table to ensure that it can be used for other
'countries as well
If [Country_Short] = "FRA" Then
    DBS.Open "[Local]", ADOC, adOpenKeyset, adLockOptimistic
    DBS.AddNew
    DBS.Fields(0).value = PK + 1
    For j = 1 To 20
        DBS.Fields(j).value = Worksheets("Portfolio").Cells(142 + j, intColumn).value
    Next
    DBS.Update
    DBS.Close
End If

'Close of database connection
ADOC.Close

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"

'Message in case of successful export
If [Country_Short] = "USA" Then
    MsgBox "Deal was successfully stored in repository. Unique Repository number is " & PK + 1 & " and Quote ID " & Max + 1 & "."
Else
    MsgBox "Deal was successfully stored in repository. Unique Repository number is " & PK + 1
End If

Exit Sub

'Error message in case of abort
Fehler:
If Err.Number = 3024 Then
    Debug.Print Err.Description
    MsgBox "Database was not found. Please check repository connection.", 0, "An error occured."
Else
    Debug.Print Err.Description
    MsgBox "An error occured. Deals could not be stored"
End If

'closing of recordset conncetion if still open in case of abort
If DBS Is Nothing Then
Else
    DBS.Close
End If

'closing of database conncetion if still open in case of abort
If ADOC Is Nothing Then
Else
    ADOC.Close
End If

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"

End Sub

Private Sub CRCExport_Click()

Dim Mappe As Workbook
Dim Tabelle As Worksheet
Dim Sheetname As String
Dim runvar As String
Dim tempstring As String
Dim intColumn As Integer
Dim dealnr As Integer

'copy necessary infos to CRCUpload sheet
Worksheets("CRCUpload").Unprotect Password:="Blattschutz"
Worksheets("CRCUpload").Range("A2:P2").Clear

'Check which deal is selected and shall be exported
If OptionButton1 = True Then intColumn = 10
If OptionButton2 = True Then intColumn = 13
If OptionButton3 = True Then intColumn = 16
If OptionButton4 = True Then intColumn = 19
If OptionButton5 = True Then intColumn = 22
If OptionButton6 = True Then intColumn = 25
If OptionButton7 = True Then intColumn = 28
If OptionButton8 = True Then intColumn = 31
If OptionButton9 = True Then intColumn = 34
If OptionButton10 = True Then intColumn = 37
If OptionButton11 = True Then intColumn = 40
If OptionButton12 = True Then intColumn = 43
If OptionButton13 = True Then intColumn = 46
If OptionButton14 = True Then intColumn = 49
If OptionButton15 = True Then intColumn = 52

dealnr = (intColumn - 7) / 3

If Worksheets("Portfolio").Cells(11, intColumn) = "" Then
    MsgBox ("No Data for Deal " & CStr(dealnr) & " available")
    Exit Sub
End If

'Country initialized
Worksheets("CRCUpload").Range("A2").value = [Country_Short]
'Deal Currency
Worksheets("CRCUpload").Range("B2").value = Left([Deal_Currency], 3)
'Average Deal Rate
Worksheets("CRCUpload").Range("C2").value = Worksheets("Portfolio").Range("F24") / 100
'Match Funding Rate - MFR
Worksheets("CRCUpload").Range("D2").value = Worksheets("Portfolio").Range("F25") / 100
'Interest Margin
Worksheets("CRCUpload").Range("E2").value = Worksheets("Portfolio").Range("F26") / 100
'Credit Risk Cost
Worksheets("CRCUpload").Range("F2").value = Worksheets("Portfolio").Range("F29") / 100
'Operational Cost
Worksheets("CRCUpload").Range("G2").value = Worksheets("Portfolio").Range("F30") / 100
'Additional Proceeds
Worksheets("CRCUpload").Range("H2").value = Format(Worksheets("Portfolio").Range("F31"), "0.#0") / 100
'Net Margin
Worksheets("CRCUpload").Range("I2").value = Worksheets("Portfolio").Range("F32") / 100
'Economic Capital
Worksheets("CRCUpload").Range("J2").value = Worksheets("Portfolio").Range("F33") / 100
'RoRAC
Worksheets("CRCUpload").Range("K2").value = Worksheets("Portfolio").Range("F35") / 100

'Expected Business ~ Total Amount to Finance
'Worksheets("CRCUpload").Range("L2").value = Worksheets("Portfolio").Range("F19")
Worksheets("CRCUpload").Range("L2").value = ""

'Present Value Outstanding
Worksheets("CRCUpload").Range("M2").value = Worksheets("Portfolio").Range("F128") / 100

'If blended Rorac is enabled, then these will be delivered too
If [Anz_Dea_Rea] > 0 Then
    
    'Check which deal is dealer retail
    Dim posOfDealerRetail As Variant
    Range("J11").Activate
    
    If Worksheets("Portfolio").Range("BC10").value = 0 Then
        For i = 1 To 10
            If (ActiveCell.value = "Dealer Retail Business") Or (ActiveCell.value = "Dealer Retail Business (Detailed)") Then
                posOfDealerRetail = Mid(CStr(ActiveCell.Address), 1, InStrRev(CStr(ActiveCell.Address), "$"))
                Exit For
            End If
             ActiveCell.Offset(0, 2).Select
        Next i
    Else
        For i = 1 To 15
            If (ActiveCell.value = "Dealer Retail Business") Or (ActiveCell.value = "Dealer Retail Business (Detailed)") Then
                posOfDealerRetail = Mid(CStr(ActiveCell.Address), 1, InStrRev(CStr(ActiveCell.Address), "$"))
                Exit For
            End If
             ActiveCell.Offset(0, 2).Select
        Next i
    End If
    
    'RoRAC - Retail
    runvar = CStr(posOfDealerRetail) + "35"
    Worksheets("CRCUpload").Range("N2").value = Worksheets("Portfolio").Range(runvar) / 100

    'Blended RoRAC
    tempstring = Worksheets("Portfolio").Range("H35")
    'tempstring = Mid(tempstring, 1, InStr(1, tempstring, "%") - 2)
    Worksheets("CRCUpload").Range("O2").value = CDbl(tempstring)
    
    'FinancedAmount
    'Worksheets("CRCUpload").Range("P2").value = Worksheets("Portfolio").Range("F135")
    Worksheets("CRCUpload").Range("P2").value = ""
End If

'Save the CSV in the international formatting standard, the delimiter is the comma ","

Dim CRCUpload As Variant
CRCUpload = Application.GetSaveAsFilename("", "CSV (*.csv),*.csv")
If CRCUpload = False Then Exit Sub
Application.ScreenUpdating = False
Application.DisplayAlerts = False
Worksheets("CRCUpload").Visible = True
Set Tabelle = Application.Sheets("CRCUpload")
Set Mappe = Workbooks.Add
Sheetname = Mappe.Sheets(1).Name

Tabelle.Copy Before:=Mappe.Worksheets(Worksheets.Count)
Mappe.Sheets(Sheetname).Delete

Application.DisplayAlerts = True
On Error GoTo error_save
Mappe.SaveAs CRCUpload, FileFormat:=XlFileFormat.xlCSVWindows, Local:=False
Application.DisplayAlerts = False
Mappe.Close
Worksheets("CRCUpload").Protect Password:="Blattschutz"
Worksheets("CRCUpload").Visible = False
Application.DisplayAlerts = True
Application.ScreenUpdating = True
Exit Sub

error_save:
MsgBox "An error occured while saving", vbCritical
On Error Resume Next
Application.DisplayAlerts = False
Mappe.Close savechanges:=False
Worksheets("CRCUpload").Protect Password:="Blattschutz"
Worksheets("CRCUpload").Visible = False
Application.DisplayAlerts = True
Application.ScreenUpdating = True

End Sub

'--------------------------------------
'NEW----------------------------------
Private Sub ExtraDeals_Click()

' Show extra deals
If ExtraDeals.value = True Then
    unhideFlag = "false"
    Worksheets("Portfolio").Unprotect Password:="Blattschutz"
    Worksheets("Portfolio").Columns("AM").ColumnWidth = 1
    Worksheets("Portfolio").Range("BC10") = 1
    ExtraDeals.Caption = "Hide 10+ Deals"
    ActiveSheet.PageSetup.PrintArea = "$A$1:$BA$37"
    Worksheets("Portfolio").Columns("AN:BA").Hidden = False
    OptionButton11.Top = 76.5
    OptionButton11.Visible = True
    
    OptionButton12.Top = 76.5
    OptionButton12.Visible = True
    
    OptionButton13.Top = 76.5
    
    OptionButton13.Visible = True
    
    OptionButton14.Top = 76.5
    OptionButton14.Visible = True
    
    OptionButton15.Top = 76.5
    OptionButton15.Visible = True
    
    If [Anz_Dea_Rea].value = 0 Then
        OptionButton11.Left = 1545
        OptionButton12.Left = 1665
        OptionButton13.Left = 1785
        OptionButton14.Left = 1905
        OptionButton15.Left = 2025
    Else
        OptionButton11.Left = 1625
        OptionButton12.Left = 1745
        OptionButton13.Left = 1865
        OptionButton14.Left = 1990
        OptionButton15.Left = 2110
    End If
    
    Worksheets("Portfolio").ScrollArea = "A1:BA38"
    Worksheets("Portfolio").Range("BC10") = 1
    Worksheets("Portfolio").Protect Password:="Blattschutz"
End If

' Hide extra deals and remove dealer retail business
If ExtraDeals.value = False Then
    unhideFlag = "true"
    Worksheets("Portfolio").Unprotect Password:="Blattschutz"
    ActiveSheet.PageSetup.PrintArea = "$A$1:$AL$37"
    ExtraDeals.Caption = "Show 10+ Deals"
    If [Anz_Dea_Rea].value = 1 Then
    Dim RetPos As Integer
    Dim delDel As Integer
    For RetPos = 40 To 52 Step 3
        If Worksheets("Portfolio").Cells(11, RetPos).value = "Dealer Retail Business" Or Worksheets("Portfolio").Cells(11, RetPos).value = "Dealer Retail Business (Detailed)" Then
            delDel = (RetPos - 7) / 3
            Select Case delDel
            Case "11"
                OptionButton11.value = True
            Case "12"
                OptionButton12.value = True
            Case "13"
                OptionButton13.value = True
            Case "14"
                OptionButton14.value = True
            Case "15"
                OptionButton15.value = True
            End Select
            Call CommandButton10_Click
            Exit For
        End If
    Next RetPos
    End If
    unhideFlag = "false"
    Worksheets("Portfolio").Unprotect Password:="Blattschutz"
    Worksheets("Portfolio").Range("BC10") = 0
     Worksheets("Portfolio").Columns("AM").ColumnWidth = 12
    If [Anz_Dea_Rea].value = 0 Then
        Worksheets("Portfolio").ScrollArea = "A1:AK37"
    Else
        Worksheets("Portfolio").ScrollArea = "A1:AL37"
    End If
    
    OptionButton11.Visible = False
    OptionButton12.Visible = False
    OptionButton13.Visible = False
    OptionButton14.Visible = False
    OptionButton15.Visible = False
    Worksheets("Portfolio").Columns("AN:BA").Hidden = True
    Worksheets("Portfolio").Range("A1").Select
    OptionButton1.value = True
    Worksheets("Portfolio").Protect Password:="Blattschutz"
End If
End Sub
'--------------------------------------
'NEW----------------------------------




'Protect Sheet and defines the ScrollArea after activation
Private Sub Worksheet_Activate()

With ActiveSheet.PageSetup
    .CenterHeader = "DFS RORAC Tool Version " & [APPVERSION]
End With


Worksheets("Portfolio").Unprotect Password:="Blattschutz"
If [Anz_Dea_Rea] > 0 Then
    Worksheets("Portfolio").Columns("H:H").EntireColumn.Hidden = False
Else
    Worksheets("Portfolio").Columns("H:H").EntireColumn.Hidden = True
End If

'Button for mexican or us quote sheet
If [Country_Short] = "MEX" Or [Country_Short] = "USA" Then
    CommandButton6.Visible = True
Else
    CommandButton6.Visible = False
End If

'Added by Rob on 23.08.2012 to recover PV Out formulas if deleted by some unknown event
If Range("J128").Formula = "" Then
    Range("J128").Formula = "=IF(J117<>"""",J117/J126,"""")"
End If

If Range("M128").Formula = "" Then
    Range("M128").Formula = "=IF(M117<>"""",M117/M126,"""")"
End If

If Range("P128").Formula = "" Then
    Range("P128").Formula = "=IF(P117<>"""",P117/P126,"""")"
End If

If Range("S128").Formula = "" Then
    Range("S128").Formula = "=IF(S117<>"""",S117/S126,"""")"
End If

If Range("V128").Formula = "" Then
    Range("V128").Formula = "=IF(V117<>"""",V117/V126,"""")"
End If

If Range("Y128").Formula = "" Then
    Range("Y128").Formula = "=IF(Y117<>"""",Y117/Y126,"""")"
End If

If Range("AB128").Formula = "" Then
    Range("AB128").Formula = "=IF(AB117<>"""",AB117/AB126,"""")"
End If

If Range("AE128").Formula = "" Then
    Range("AE128").Formula = "=IF(AE117<>"""",AE117/AE126,"""")"
End If

If Range("AH128").Formula = "" Then
    Range("AH128").Formula = "=IF(AH117<>"""",AH117/AH126,"""")"
End If

If Range("AK128").Formula = "" Then
    Range("AK128").Formula = "=IF(AK117<>"""",AK117/AK126,"""")"
End If
'End


Worksheets("Portfolio").Protect Password:="Blattschutz"
If Worksheets("Portfolio").Columns("AN:BA").Hidden = False Then
    Worksheets("Portfolio").ScrollArea = "A1:BA37"
Else
    Worksheets("Portfolio").ScrollArea = "A1:AN37"
End If
Worksheets("Portfolio").Cells(2, 4).Select

'added by carsten for USTF
If [Country_Short] = "USA" Then
Worksheets("portfolio").Range("D6").value = "=getusername()"
Worksheets("portfolio").Range("D6").Calculate
End If
'end
End Sub

'Copies Deal to BOM Deals View
Private Sub CommandButton2_Click()

Dim wksPortfolio As Worksheet
Dim wksBOMDeals As Worksheet
Dim clm As Integer

Set wksPortfolio = Sheets("Portfolio")
Set wksBOMDeals = Sheets("BOM Deals")

If wksPortfolio.Range("f22") = "" Then
    MsgBox ("No Deal Data available")
    Exit Sub
End If

clm = 4

'Determine column where deal shall be stored
Do Until wksBOMDeals.Cells(7, clm).value = "" Or wksPortfolio.Range("f39").value = wksBOMDeals.Cells(7, clm).value
    clm = clm + 1
    If clm > 8 Then
        MsgBox ("Too many Deals, please delete one or consolidate with another deal")
        Exit Sub
    End If
Loop

'If a deal for the appropriated country already exists, Asks if it shall be overwrited
If wksPortfolio.Range("f39").value = wksBOMDeals.Cells(7, clm).value Then
    intSure = MsgBox("Existing Country Deal will be replaced", vbOKCancel, "Country Deal already exists")
    If intSure = 2 Then
        Exit Sub
    End If
End If

With Application
        .Calculate
        .Calculation = xlManual
        .MaxChange = 0.001
        .ScreenUpdating = False
End With

ActiveWorkbook.PrecisionAsDisplayed = False

Worksheets("BOM Deals").Unprotect Password:="Blattschutz"

'Depending on the existence of Dealer Retail Business the respective columns are visible or hidden
If [Anz_Dea_Rea] > 0 Then
    Worksheets("BOM Deals").Rows("19:20").EntireRow.Hidden = False
    Worksheets("BOM Deals").Range("B18").value = "Wholesale RORAC"
    wksBOMDeals.Cells(19, clm).value = wksPortfolio.Range("F138").value / 100
    wksBOMDeals.Cells(20, clm).value = wksPortfolio.Range("H35").value
Else
    Worksheets("BOM Deals").Rows("19:20").EntireRow.Hidden = True
    Worksheets("BOM Deals").Range("B18").value = "RORAC"
End If
'Copies deal information from Portfolio to BOM-Deal sheet
With wksBOMDeals
    .Cells(8, 2).value = "(Acquisition RORAC calculation as of " & wksPortfolio.Range("C38").value & ")"
    .Cells(7, clm).value = wksPortfolio.Range("f39").value
    .Cells(9, clm).value = wksPortfolio.Range("f24").value * 0.01
    .Cells(10, clm).value = wksPortfolio.Range("f25").value * 0.01
    .Cells(11, clm).value = wksPortfolio.Range("f28").value * 0.01
    .Cells(12, clm).value = wksPortfolio.Range("f29").value * 0.01
    .Cells(13, clm).value = wksPortfolio.Range("f101").value * 100
    .Cells(14, clm).value = wksPortfolio.Range("f30").value * 0.01
    .Cells(15, clm).value = wksPortfolio.Range("f31").value * 0.01
    .Cells(16, clm).value = wksPortfolio.Range("f32").value * 0.01
    .Cells(17, clm).value = wksPortfolio.Range("f33").value * 0.01
    .Cells(18, clm).value = wksPortfolio.Range("f35").value * 0.01
    .Cells(22, clm).value = wksPortfolio.Range("f129").value
End With
    
'Protects Sheet after finishing
With Application
    .Calculate
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False

Worksheets("BOM Deals").Protect Password:="Blattschutz"

End Sub


Private Sub CommandButton3_Click()
Sheets("BOM Deals").Select
End Sub


' Deletes All Deals within Portfolio View
Private Sub CommandButton1_Click()

Dim intSure As Integer
Dim intColumn As Integer
Dim dealnr As Integer

intSure = MsgBox(" Are you sure?", vbYesNo, "Delete All Deals")
If intSure = 7 Then
    Exit Sub
End If

Worksheets("Portfolio").Unprotect Password:="Blattschutz"

With Application
    .Calculate
    .Calculation = xlManual
    .MaxChange = 0.001
    .ScreenUpdating = False
End With

ActiveWorkbook.PrecisionAsDisplayed = False

'Mexico only
For intColumn = 10 To 37 Step 3
    If Worksheets("New Input Mask").Range("E5").value = "MEX" Then
        Worksheets("Local_output").Unprotect Password:="Blattschutz"
        Worksheets("Local_Output").Cells(20, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(21, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(22, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(23, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(24, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(25, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(27, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(28, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(29, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(30, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(31, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(32, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(33, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(34, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(35, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(36, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(37, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(39, intColumn - 5).value = ""
        Dim Y As Integer
        For Y = 90 To 229
            Worksheets("Local_Output").Cells(Y, intColumn - 5).value = ""
        Next
        Worksheets("Local_Output").Range("AJ3:GU120").ClearContents
        Worksheets("Local_output").Protect Password:="Blattschutz"
    End If
    'END MEXICO ONLY - ADDED CS
Next

With Worksheets("Portfolio")
    .Cells(2, 4).Select
    Selection.ClearContents
    .Cells(4, 4).Select
    Selection.ClearContents
    .Cells(6, 4).Select
    Selection.ClearContents
    For intColumn = 10 To 53 Step 3
        .Cells(5, intColumn).Select
        Selection.ClearContents
        .Cells(9, intColumn).Select
        Selection.ClearContents
        .Cells(10, intColumn).Select
        Selection.ClearContents
        Selection.ClearComments
        .Cells(11, intColumn).Select
        Selection.ClearContents
        .Cells(12, intColumn).Select
        Selection.ClearContents
        .Range(Cells(13, intColumn), Cells(127, intColumn)).Select
        Selection.ClearContents
        .Range(Cells(143, intColumn), Cells(162, intColumn)).Select
        Selection.ClearContents
    Next
    .Range("d2").Select
    .Columns("H:H").EntireColumn.Hidden = True
End With

Application.CalculateFullRebuild
Worksheets("Portfolio").Protect Password:="Blattschutz"
With Application
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
End Sub

'Copies a selected Deal back to Input Mask

Private Sub CommandButton9_Click()

Dim wksPortfolio As Worksheet
Dim wksIndex As Worksheet
Dim wksinput As Worksheet
Dim intColumn As Integer

If OptionButton1 = True Then intColumn = 10
If OptionButton2 = True Then intColumn = 13
If OptionButton3 = True Then intColumn = 16
If OptionButton4 = True Then intColumn = 19
If OptionButton5 = True Then intColumn = 22
If OptionButton6 = True Then intColumn = 25
If OptionButton7 = True Then intColumn = 28
If OptionButton8 = True Then intColumn = 31
If OptionButton9 = True Then intColumn = 34
If OptionButton10 = True Then intColumn = 37
If OptionButton11 = True Then intColumn = 40
If OptionButton12 = True Then intColumn = 43
If OptionButton13 = True Then intColumn = 46
If OptionButton14 = True Then intColumn = 49
If OptionButton15 = True Then intColumn = 52


'Check if deal data are available and selected deal is not dealer retail business
If Worksheets("Portfolio").Cells(11, intColumn) = "" Then
    MsgBox ("No Deal Data available")
    Exit Sub
End If

If Worksheets("Portfolio").Cells(11, intColumn) = "Dealer Retail Business" Or Worksheets("Portfolio").Cells(11, intColumn) = "Dealer Retail Business (Detailed)" Then
    MsgBox ("Dealer Retail Business cannot be recalculated")
    Exit Sub
End If

Call unprotectInput
Worksheets("Portfolio").Unprotect Password:="Blattschutz"

Set wksPortfolio = Sheets("Portfolio")
Set wksinput = Sheets("New Input Mask")
Set wksIndex = Sheets("Index")

With wksinput
    If [Country_Short] <> "MEX" Then
    .Range("E5").value = wksPortfolio.Cells(39, intColumn).value
    .Range("L5").value = wksPortfolio.Cells(40, intColumn).value
    End If
    .Unprotect Password:="Blattschutz"
End With
Application.Calculation = xlCalculationManual
Application.ScreenUpdating = False
Application.EnableEvents = False

'check if deal was calculated with a manual cash flow or an accelerated payment
If wksPortfolio.Cells(52, intColumn).value = "1" Then
    MsgBox "Please note that this deals was calculated based on a manual Cash Flow that was not stored." & vbCrLf & "Please enter Cash Flow again."
End If

If wksPortfolio.Cells(76, intColumn).value = "Yes" Then
    MsgBox "Please note that this deals was calculated with an Accelerated Payment that was not stored." & vbCrLf & "Please enter again."
End If

With wksPortfolio
    wksinput.Range("L8").value = .Cells(9, intColumn).value
    wksinput.Range("E8").value = .Cells(125, intColumn).value
    wksinput.Range("E19").value = .Cells(108, intColumn).value
    wksinput.Range("H47").value = .Cells(14, intColumn).value
    wksinput.Range("L17").value = .Cells(15, intColumn).value
    wksinput.Range("E10").value = .Cells(41, intColumn).value
    wksinput.Range("h60").value = .Cells(42, intColumn).value
    wksinput.Range("h62").value = .Cells(43, intColumn).value
    wksinput.Range("h64").value = .Cells(45, intColumn).value
    wksinput.Range("E12").value = .Cells(47, intColumn).value
    wksinput.Range("E17").value = .Cells(48, intColumn).value
    wksinput.Range("U17").value = .Cells(49, intColumn).value
    wksinput.Range("Q19").value = .Cells(51, intColumn).value
    Application.EnableEvents = True
    wksinput.Range("E25").value = .Cells(53, intColumn).value
    Application.EnableEvents = False
    Call unprotectInput
    wksinput.Range("D27").value = .Cells(54, intColumn).value
    wksinput.Range("G27").value = .Cells(55, intColumn).value
    wksinput.Range("h29").value = .Cells(56, intColumn).value
    wksinput.Range("H31").value = .Cells(57, intColumn).value
    wksinput.Range("H39").value = .Cells(61, intColumn).value
    wksinput.Range("H41").value = .Cells(62, intColumn).value
    wksinput.Range("H43").value = .Cells(63, intColumn).value
    wksinput.Range("e49").value = .Cells(67, intColumn).value
    wksinput.Range("h49").value = .Cells(68, intColumn).value
    wksinput.Range("e52").value = .Cells(69, intColumn).value
    wksinput.Range("h52").value = .Cells(70, intColumn).value
    wksinput.Range("e54").value = .Cells(71, intColumn).value
    wksinput.Range("h54").value = .Cells(73, intColumn).value
    wksinput.Range("e58").value = .Cells(74, intColumn).value
    wksinput.Range("H58").value = .Cells(75, intColumn).value
    'wksinput.Range("E60").value = .Cells(76, intColumn).value
    wksinput.Range("E64").value = .Cells(78, intColumn).value
    wksinput.Range("E66").value = .Cells(79, intColumn).value
    wksinput.Range("E68").value = .Cells(80, intColumn).value
    wksinput.Range("E70").value = .Cells(81, intColumn).value
    wksIndex.Range("M163").value = .Cells(93, intColumn).value
    wksIndex.Range("M130").value = .Cells(94, intColumn).value
    wksIndex.Range("M131").value = .Cells(95, intColumn).value
    wksIndex.Range("M132").value = .Cells(96, intColumn).value
    wksIndex.Range("M133").value = .Cells(97, intColumn).value
    wksIndex.Range("M134").value = .Cells(98, intColumn).value
    wksIndex.Range("M135").value = .Cells(99, intColumn).value
    wksIndex.Range("M136").value = .Cells(100, intColumn).value
    wksIndex.Range("M137").value = .Cells(101, intColumn).value
    wksIndex.Range("M138").value = .Cells(102, intColumn).value
    wksIndex.Range("M139").value = .Cells(103, intColumn).value
    wksIndex.Range("M140").value = .Cells(104, intColumn).value
    wksIndex.Range("M141").value = .Cells(105, intColumn).value
    wksIndex.Range("M142").value = .Cells(106, intColumn).value
    wksIndex.Range("M143").value = .Cells(107, intColumn).value
    wksinput.Range("Q12").value = .Cells(109, intColumn).value
    wksIndex.Range("M144").value = .Cells(110, intColumn).value
    wksIndex.Range("M145").value = .Cells(111, intColumn).value
    wksIndex.Range("M146").value = .Cells(112, intColumn).value
    wksIndex.Range("M148").value = .Cells(113, intColumn).value
    wksIndex.Range("M153").value = .Cells(114, intColumn).value
    wksinput.Range("L58").value = .Cells(115, intColumn).value
    wksinput.Range("L74").value = .Cells(115, intColumn).value
    Application.CalculateFull
    'for us rating/scoring information will be copied to local sheet
    If [Country_Short] = "USA" Then
        [R_S_US] = .Cells(86, intColumn).value
        [R_S_US_Value] = .Cells(41, intColumn).value
        wksinput.Range("E10").value = ""
    End If
    'for france insurance information will be copied to local sheet
    If [Country_Short] = "FRA" Then
        [Differe] = .Cells(143, intColumn)
        [Day_Differe] = .Cells(144, intColumn)
        [Assurance] = .Cells(145, intColumn)
        [Assurance1] = .Cells(146, intColumn)
        [Assurance_Typ1] = .Cells(147, intColumn)
        [Produits_financiers1] = .Cells(148, intColumn)
        [Assurance2] = .Cells(149, intColumn)
        [Assurance_Typ2] = .Cells(150, intColumn)
        [Produits_financiers2] = .Cells(151, intColumn)
        [Per_Annee] = .Cells(153, intColumn)
        [Duree_Contrat] = .Cells(154, intColumn)
        [Montant_annuel] = .Cells(155, intColumn)
        [perc_dur_contrat] = .Cells(156, intColumn)
        [Commission_Fixed] = .Cells(157, intColumn)
     End If
End With

'Shows or hide Manual PD Field depending on the setting
If Worksheets("New Input Mask").Range("E10").value = "" Then
    Worksheets("New Input Mask").Rows("12:13").Hidden = False
Else
    Worksheets("New Input Mask").Rows("12:13").Hidden = True
End If

Application.EnableEvents = True
Application.ScreenUpdating = False
Worksheets("New Input Mask").ComboBox1.value = wksPortfolio.Cells(59, intColumn).value
Worksheets("New Input Mask").ComboBox4.value = wksPortfolio.Cells(64, intColumn).value
Worksheets("New Input Mask").ComboBox2.value = wksPortfolio.Cells(66, intColumn).value
Worksheets("New Input Mask").ComboBox3.value = wksPortfolio.Cells(72, intColumn).value
Worksheets("New Input Mask").ComboBox5.value = wksPortfolio.Cells(46, intColumn).value
Worksheets("New Input Mask").ComboBox6.value = wksPortfolio.Cells(116, intColumn).value
wksinput.Range("L19").value = wksPortfolio.Cells(50, intColumn).value
wksinput.Range("U19").value = wksPortfolio.Cells(44, intColumn).value
wksIndex.Range("C323").value = wksPortfolio.Cells(119, intColumn).value
wksIndex.Range("D323").value = wksPortfolio.Cells(120, intColumn).value
wksIndex.Range("E323").value = wksPortfolio.Cells(121, intColumn).value
wksIndex.Range("C324").value = wksPortfolio.Cells(122, intColumn).value
wksIndex.Range("D324").value = wksPortfolio.Cells(123, intColumn).value
wksIndex.Range("E324").value = wksPortfolio.Cells(124, intColumn).value

Application.EnableEvents = False

wksinput.Range("E33").value = wksPortfolio.Cells(58, intColumn).value
wksinput.Range("E37").value = wksPortfolio.Cells(60, intColumn).value
wksinput.Range("E45").value = wksPortfolio.Cells(65, intColumn).value

'Set Accelerated Payment to No
wksinput.Range("E60").value = "No"
wksinput.Range("E61").Locked = True
wksinput.Range("E62").Locked = True
Worksheets("New Input Mask").ComboBox7.Visible = False
Worksheets("Index").Range("Accelerated_Payment_Flag") = 0

Application.ScreenUpdating = False
Worksheets("Index").Range("Manual_CF_Flag") = 0

intSure = MsgBox("Do you want to keep deal on Portfolio?", vbYesNo, "Delete Deal?")
If intSure = 7 Then
    Worksheets("Portfolio").Unprotect Password:="Blattschutz"
    Worksheets("Portfolio").Cells(5, intColumn).value = ""
    Worksheets("Portfolio").Cells(9, intColumn).value = ""
    Worksheets("Portfolio").Cells(10, intColumn).value = ""
    Worksheets("Portfolio").Cells(10, intColumn).ClearComments
    Worksheets("Portfolio").Cells(11, intColumn).value = ""
    Worksheets("Portfolio").Cells(12, intColumn).value = ""
    Worksheets("Portfolio").Range(Cells(13, intColumn), Cells(128, intColumn)).ClearContents
    
    ' MEXICO ONLY - ADDED CS
    If Worksheets("New Input Mask").Range("E5").value = "MEX" Then
        Worksheets("Local_output").Unprotect Password:="Blattschutz"
        Worksheets("Local_Output").Cells(20, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(21, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(22, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(23, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(24, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(25, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(27, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(28, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(29, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(30, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(31, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(32, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(33, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(34, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(35, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(36, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(37, intColumn - 5).value = ""
        Worksheets("Local_Output").Cells(39, intColumn - 5).value = ""
        Dim Y As Integer
        For Y = 90 To 229
            Worksheets("Local_Output").Cells(Y, intColumn - 5).value = ""
        Next
        
        Dim xrow As Integer
        Dim zcol As Integer
        For xrow = 3 To 87
            For zcol = (19 + ((intColumn - 7) / 3) * 17) To (19 + ((intColumn - 7) / 3) * 17) + 8
                Worksheets("Local_Output").Cells(xrow, zcol).value = ""
            Next
        Next
        Worksheets("Local_output").Protect Password:="Blattschutz"
        
    End If
    ' END MEXICO ONLY - ADDED CS

End If
Application.Calculate
Application.Calculation = xlCalculationManual
Call prcStartCalculation

Worksheets("New Input Mask").Select
Worksheets("New Input Mask").Range("e5").Activate
Worksheets("New Input Mask").CommandButton23.Visible = False
Worksheets("New Input Mask").CommandButton22.Caption = "Use MCF"

Call LGD_button_CalcDate

Application.EnableEvents = True
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "RORAC was successfully calculated"


Worksheets("Portfolio").Protect Password:="Blattschutz"


End Sub

'Opens Input Sheet
Private Sub CommandButton12_Click()

Worksheets("New Input Mask").Select
Worksheets("New Input Mask").[e8].Activate
Application.EnableEvents = True
End Sub

'Deletes selected Deal
Private Sub CommandButton10_Click()

Dim intSure As Integer
Dim intColumn As Integer
Dim dealnr As Integer
Dim wksoutput As Worksheet 'MEXICO ONLY'

If OptionButton1 = True Then intColumn = 10
If OptionButton2 = True Then intColumn = 13
If OptionButton3 = True Then intColumn = 16
If OptionButton4 = True Then intColumn = 19
If OptionButton5 = True Then intColumn = 22
If OptionButton6 = True Then intColumn = 25
If OptionButton7 = True Then intColumn = 28
If OptionButton8 = True Then intColumn = 31
If OptionButton9 = True Then intColumn = 34
If OptionButton10 = True Then intColumn = 37
If OptionButton11 = True Then intColumn = 40
If OptionButton12 = True Then intColumn = 43
If OptionButton13 = True Then intColumn = 46
If OptionButton14 = True Then intColumn = 49
If OptionButton15 = True Then intColumn = 52

dealnr = (intColumn - 7) / 3

If Worksheets("Portfolio").Cells(11, intColumn) = "" Then
    MsgBox ("No Deal Data available")
    Exit Sub
End If

If unhideFlag <> "true" Then
    intSure = MsgBox(" Are you sure?", vbYesNo, "Delete Deal" & Str(dealnr))
    If intSure = 7 Then
        Exit Sub
    End If
End If

Worksheets("Portfolio").Unprotect Password:="Blattschutz"
Application.ScreenUpdating = False

Worksheets("Portfolio").Cells(5, intColumn).Select
Selection.ClearContents
Worksheets("Portfolio").Cells(9, intColumn).Select
Selection.ClearContents
Worksheets("Portfolio").Cells(10, intColumn).Select
Selection.ClearContents
Selection.ClearComments
Worksheets("Portfolio").Cells(11, intColumn).Select
Selection.ClearContents
Worksheets("Portfolio").Cells(12, intColumn).Select
Selection.ClearContents
Worksheets("Portfolio").Range(Cells(13, intColumn), Cells(127, intColumn)).Select
Selection.ClearContents
Worksheets("Portfolio").Range(Cells(143, intColumn), Cells(162, intColumn)).Select
Selection.ClearContents

If [Anz_Dea_Rea] > 0 Then
    Worksheets("Portfolio").Columns("H:H").EntireColumn.Hidden = False
Else
    Worksheets("Portfolio").Columns("H:H").EntireColumn.Hidden = True
End If

' MEXICO ONLY - ADDED by Carsten Sturmann
If Worksheets("New Input Mask").Range("E5").value = "MEX" Then
Worksheets("Local_output").Unprotect Password:="Blattschutz"
    Worksheets("Local_Output").Cells(20, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(21, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(22, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(23, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(24, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(25, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(27, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(28, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(29, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(30, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(31, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(32, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(33, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(34, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(35, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(36, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(37, intColumn - 5).value = ""
    Worksheets("Local_Output").Cells(39, intColumn - 5).value = ""
    Dim Y As Integer
    For Y = 90 To 229
        Worksheets("Local_Output").Cells(Y, intColumn - 5).value = ""
    Next

    Dim xrow As Integer
    Dim zcol As Integer
    For xrow = 3 To 87
        For zcol = (19 + ((intColumn - 7) / 3) * 17) To (19 + ((intColumn - 7) / 3) * 17) + 8
            Worksheets("Local_Output").Cells(xrow, zcol).value = ""
        Next
    Next

Worksheets("Local_output").Protect Password:="Blattschutz"
End If

' END MEXICO ONLY - ADDED CS

Application.CalculateFullRebuild

Worksheets("Portfolio").Range("d10").Select
Worksheets("Portfolio").Protect Password:="Blattschutz"
Application.ScreenUpdating = True

End Sub







--- Macro File: US_Cust.bas ---
Attribute VB_Name = "US_Cust"
'--------- USTF CODE ONLY --------

Option Explicit

'Constant for Database connection string

Private Const glob_DBPath = "cst_list.mdb"
'Private Const glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & glob_DBPath & ";"

Private Sub RetrieveRecordset(strSQL As String, clTrgt As Range)
'Author       : Ken Puls (www.excelguru.ca)
'Macro Purpose: To retrieve a recordset from a database (via an SQL query) and place
'               it in the supplied worksheet range
'NOTE         : Requires a reference to "Microsoft ActiveX Data Objects 2.x Library"
'               (Developed with reference to version 2.0 of the above)

    Dim cnt As New ADODB.Connection
    Dim rst As New ADODB.Recordset
    Dim rcArray As Variant
    Dim lFields As Long
    Dim lRecrds As Long
    Dim lCol As Long
    Dim lRow As Long
    Dim reppath_drive As String
    Dim glob_sConnect As String

reppath_drive = Left(Range("Path_Repository"), InStrRev(Range("Path_Repository"), "\")) & glob_DBPath
glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & reppath_drive & ";"


With cnt
      .Provider = "Microsoft.Jet.OLEDB.4.0"
      .Properties("Jet OLEDB:Database Password") = "teamwork"
      .Mode = adModeReadWrite
      .Open glob_sConnect
   End With


    'Open connection to the database
    'cnt.Open glob_sConnect

    'Open recordset based on Orders table
    rst.Open strSQL, cnt

    'Count the number of fields to place in the worksheet
    lFields = rst.Fields.Count

    'Check version of Excel
    If Val(Mid(Application.Version, 1, InStr(1, Application.Version, ".") - 1)) > 8 Then
        'EXCEL 2000 or 2002: Use CopyFromRecordset

        'Copy the recordset from the database
        On Error Resume Next
        clTrgt.CopyFromRecordset rst
        
        'CopyFromRecordset will fail if the recordset contains an OLE
        'object field or array data such as hierarchical recordsets
        If Err.Number <> 0 Then GoTo EarlyExit
    
    Else
        'EXCEL 97 or earlier: Use GetRows then copy array to Excel

        'Copy recordset to an array
        rcArray = rst.GetRows

        'Determine number of records (adds 1 since 0 based array)
        lRecrds = UBound(rcArray, 2) + 1

        'Check the array for contents that are not valid when
        'copying the array to an Excel worksheet
        For lCol = 0 To lFields - 1
            For lRow = 0 To lRecrds - 1
                'Take care of Date fields
                If IsDate(rcArray(lCol, lRow)) Then
                    rcArray(lCol, lRow) = Format(rcArray(lCol, lRow))
                    'Take care of OLE object fields or array fields
                ElseIf IsArray(rcArray(lCol, lRow)) Then
                    rcArray(lCol, lRow) = "Array Field"
                End If
            Next lRow
        Next lCol

        'Transpose and place the array in the worksheet
        clTrgt.Resize(lRecrds, lFields).value = TransposeDim(rcArray)
    End If

EarlyExit:
    'Close and release the ADO objects
    rst.Close
    cnt.Close
    Set rst = Nothing
    Set cnt = Nothing
    On Error GoTo 0

End Sub

Private Function TransposeDim(v As Variant) As Variant
'Function Purpose:  Transpose a 0-based array (v)

    Dim X As Long, Y As Long, Xupper As Long, Yupper As Long
    Dim tempArray As Variant

    Xupper = UBound(v, 2)
    Yupper = UBound(v, 1)

    ReDim tempArray(Xupper, Yupper)
    For X = 0 To Xupper
        For Y = 0 To Yupper
            tempArray(X, Y) = v(Y, X)
        Next Y
    Next X

    TransposeDim = tempArray

End Function
Sub GetRecords()
'Macro Purpose: To retrieve a recordset to an Excel worksheet

    Dim sSQLQry As String
    Dim rngTarget As Range
    
    Dim MyInput As String
    MyInput = InputBox("ENTER CUSTOMER NAME", _
    "USER INPUT", "INPUT")

    'MyInput = "%" & MyInput & "%"

    If MyInput = "Enter your input text HERE" Or _
    MyInput = "" Then
     Exit Sub
     End If



    'Generate the SQL query and set the range to place the data in
    sSQLQry = "SELECT Top 10 tbl1.customername, tbl1.rating FROM tbl1 where tbl1.customername like '%" & MyInput & "%';"
    'ActiveSheet.Cells.ClearContents
    Range("H12:I21").Cells.ClearContents
    Set rngTarget = ActiveSheet.Range("H12")

    'Retrieve the records
    Call RetrieveRecordset(sSQLQry, rngTarget)

End Sub

Sub GetRecords2()
'Macro Purpose: To retrieve a recordset to an Excel worksheet

    Dim sSQLQry As String
    Dim rngTarget As Range
    
    Dim MyInput As String
    MyInput = InputBox("ENTER CUSTOMER NAME", _
    "USER INPUT", "INPUT")

    'MyInput = "%" & MyInput & "%"

    If MyInput = "Enter your input text HERE" Or _
    MyInput = "" Then
     Exit Sub
     End If



    'Generate the SQL query and set the range to place the data in
    sSQLQry = "SELECT Top 10 tbl2.customername, tbl2.score FROM tbl2 where tbl2.customername like '%" & MyInput & "%';"
    'ActiveSheet.Cells.ClearContents
    Range("L12:M21").Cells.ClearContents
    
    Set rngTarget = ActiveSheet.Range("L12")

    'Retrieve the records
    Call RetrieveRecordset(sSQLQry, rngTarget)

End Sub




--- Macro File: DCF_Excel.bas ---
Attribute VB_Name = "DCF_Excel"
Public Function fct_GetDCFExcel(pdblDate As Double, prngDCF As Range) As Double

    Dim lintRun As Integer
    Dim lintUp As Integer
    Dim lintDown As Integer
    
    fct_GetDCFExcel = 1

    If pdblDate <= prngDCF.Cells(1, 1) Then
        If pdblDate = prngDCF.Cells(3, 1) Then
            fct_GetDCFExcel = prngDCF.Cells(3, 3)
        Else
            fct_GetDCFExcel = fct_Linear(pdblDate - prngDCF.Cells(3, 1), prngDCF.Cells(3, 3), prngDCF.Cells(1, 3), prngDCF.Cells(3, 1) - prngDCF.Cells(3, 1), prngDCF.Cells(1, 1) - prngDCF.Cells(3, 1))
        End If
    End If
    
    lintDown = 3
    lintRun = 4
    Do While prngDCF.Cells(lintRun, 1) <= pdblDate
        If prngDCF.Cells(lintRun, 2) <> "" Then lintDown = lintRun
        lintRun = lintRun + 1
    Loop
    Do While prngDCF.Cells(lintRun, 2) = ""
        lintRun = lintRun + 1
    Loop
    lintUp = lintRun
    
    If lintDown = 3 Then lintDown = 1
    fct_GetDCFExcel = fct_Exponential(pdblDate - prngDCF.Cells(3, 1), prngDCF.Cells(lintDown, 3), prngDCF.Cells(lintUp, 3), prngDCF.Cells(lintDown, 1) - prngDCF.Cells(3, 1), prngDCF.Cells(lintUp, 1) - prngDCF.Cells(3, 1))
    

End Function



--- Macro File: BASEL_II.bas ---
Attribute VB_Name = "BASEL_II"
Const Pi = 3.141592654

Function r(ByVal pd As Single) As Single

r = 0.12 * (1 - Exp(-50 * pd)) / (1 - Exp(-50)) + 0.24 * (1 - (1 - Exp(-50 * pd)) / (1 - Exp(-50)))

End Function

Function b(ByVal pd As Single) As Single

b = (0.11852 - 0.05478 * Ln(pd)) ^ 2

End Function

Public Function UL(ByVal pd As Single, ByVal LGD As Single, ByVal m As Single) As Double

Dim r1, b1 As Single
r1 = r(pd)
b1 = b(pd)

UL = (LGD * n((1 - r1) ^ -0.5 * g(pd) + (r1 / (1 - r1)) ^ 0.5 * g(0.999)) - pd * LGD) * (1 - 1.5 * b1) ^ -1 * (1 + (m - 2.5) * b1)

End Function

Function EL(ByVal LGD As Single, ByVal pd As Single) As Double

EL = LGD * pd

End Function

Function CreditVaR(ByVal LGD As Single, ByVal pd As Single, ByVal m As Single)

CreditVaR = EL(LGD, pd) + UL(LGD, pd, m)

End Function

Function n(ByVal X As Double) As Double

n = NormalDist(X)

End Function

Function g(ByVal X As Double) As Double

g = NormalInv(X)

End Function

Public Function NormalDist(ByVal X As Double) As Double

Dim a(4) As Double, k As Double
Dim l As Double
Dim Q As Double
Dim nx As Double
Dim SND As Double
Dim j As Integer
Dim s As Double

a(0) = 0.31938153
a(1) = -0.356563782
a(2) = 1.781477937
a(3) = -1.821255978
a(4) = 1.330274429

k = 0.2316419
l = Abs(X)
Q = 1 / (1 + k * l)
nx = Density(l)
'nx == f(abs(x))

For j = 0 To 4
    s = s + a(j) * Q ^ (j + 1)
Next j

SND = 1 - nx * s

'SND = PHI(abs(x))

If (X < 0) Then
    SND = 1 - SND
End If

'NOW SND = PHI(x)

NormalDist = SND

End Function

Private Function Density(ByVal X As Double) As Double

Density = 1 / (2 * Pi) ^ 0.5 * Exp(-0.5 * X ^ 2)

End Function

Public Function NormalInv(ByVal r As Double) As Double

Dim Delta As Double

Dim X As Double, Y As Double

X = 0

Delta = 1
Do Until Delta <= 0.00000001
    Y = X - (NormalDist(X) - r) / Density(X)
    Delta = Abs(Y - X)
    X = Y
Loop
NormalInv = Y

End Function

Function Ln(ByVal X As Single) As Single

Ln = Log(X) / Log(2.71828182845905)

End Function

Function CorrelCorp(pd As Double) As Double
Dim a As Double
      
    CorrelCorp = 0.12 * (1# - Exp(-50# * pd)) / (1 - Exp(-50#))
    CorrelCorp = CorrelCorp + 0.24 * (1# - (1# - Exp(-50# * pd)) / (1 - Exp(-50#)))
    'CorrelCorp = CorrelCorp - (0.04 * (1 - ((S - 5) / 45))) 'SME adjustment

End Function
Function CorrelDealer(pd As Double) As Double
Dim a As Double
      
    CorrelDealer = 0.12 * (1# - Exp(-50# * pd)) / (1 - Exp(-50#))
    CorrelDealer = CorrelDealer + 0.24 * (1# - (1# - Exp(-50# * pd)) / (1 - Exp(-50#))) '0.48 suggested as ammendment
    'CorrelCorp = CorrelCorp - (0.04 * (1 - ((S - 5) / 45))) 'SME adjustment

End Function
Function MatCorp(pd As Double) As Double
    MatCorp = (0.11852 - 0.05478 * Log(pd)) ^ 2
End Function

Function UL_corp(pd As Double, LGD As Double, maturity As Double) As Double
    
    Dim Correl As Double
    Dim matAdjust As Double
    
    Correl = CorrelCorp(pd)
    matAdjust = MatCorp(pd)
    
    UL_corp = (((1 - Correl) ^ -0.5) * g(pd) + ((Correl / (1 - Correl)) ^ 0.5) * g(0.999))
    UL_corp = n(UL_corp) - pd
    UL_corp = UL_corp / (1# - 1.5 * matAdjust) * (1# + (maturity - 2.5) * matAdjust)
    UL_corp = UL_corp * LGD
    
End Function
Function UL_Dealer(pd As Double, LGD As Double, maturity As Double) As Double
    
    Dim Correl As Double
    Dim matAdjust As Double
    
    Correl = CorrelDealer(pd)
    matAdjust = MatCorp(pd)
    
    UL_Dealer = (((1 - Correl) ^ -0.5) * g(pd) + ((Correl / (1 - Correl)) ^ 0.5) * g(0.999))
    UL_Dealer = n(UL_Dealer) - pd
    UL_Dealer = UL_Dealer / (1# - 1.5 * matAdjust) * (1# + (maturity - 2.5) * matAdjust)
    UL_Dealer = UL_Dealer * LGD
    
End Function

Function UL_retail(pd As Double, LGD As Double) As Double
    
    Dim Correl As Double
    
    Correl = CorrelRetail(pd)
    
    UL_retail = (((1 - Correl) ^ -0.5) * g(pd) + ((Correl / (1 - Correl)) ^ 0.5) * g(0.999))
    UL_retail = n(UL_retail) - pd
    UL_retail = UL_retail * LGD
    
End Function

Function CorrelRetail(pd As Double) As Double
    
    CorrelRetail = 0.03 * (1# - Exp(-35# * pd)) / (1 - Exp(-35#))
    CorrelRetail = CorrelRetail + 0.16 * (1# - (1# - Exp(-35# * pd)) / (1 - Exp(-35#)))

End Function


Function GetCountryISO(country As String) As String

Dim i As Integer

For i = 71 To 85
    If country = Worksheets("CRISKCorrel").Range("A" + Format(i)) Then GetCountryISO = Worksheets("CRISKCorrel").Range("B" + Format(i))
Next i

End Function

Function GetCorrel(Country1 As String, Country2 As String) As Double

Dim col, row As Integer
Dim Iso1, Iso2 As String

Iso1 = GetCountryISO(Country1)
Iso2 = GetCountryISO(Country2)

For col = 2 To 67
    For row = 2 To 67
        If Worksheets("CRISKCorrel").Cells(row, 1) = Iso1 And Worksheets("CRISKCorrel").Cells(1, col) = Iso2 Then
            GetCorrel = Worksheets("CRISKCorrel").Cells(row, col)
            Exit Function
        End If
    Next row
Next col

End Function

Function CorrelCluster(Correl As Double) As Double

Dim i As Integer

For i = 88 To 96
    If Correl >= Worksheets("CRISKCorrel").Range("B" + Format(i)) And Correl <= Worksheets("CRISKCorrel").Range("C" + Format(i)) Then
        CorrelCluster = Worksheets("CRISKCorrel").Range("D" + Format(i))
        Exit Function
    End If
Next i

End Function


Function Max(val1 As Double, val2 As Double) As Double
    If val1 > val2 Then
        Max = val1
    Else
        Max = val2
    End If
End Function





--- Macro File: Tabelle9.cls ---
Attribute VB_Name = "Tabelle9"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True




--- Macro File: Tabelle15.cls ---
Attribute VB_Name = "Tabelle15"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: mdlCalculation.bas ---
Attribute VB_Name = "mdlCalculation"
Option Explicit
Global arrCalculation() As Variant

Function fctCalculation_Generation(datePayout_Date As Date, intPayment_Frequency As Integer, _
                            dateFirst_Instalment_Date_Input As Date, _
                            dblInititial_Direct_Cost As Double, _
                            dblSubsidies As Double, intCredit_Term As Integer, _
                            lContract As typContract, dblMSRP As Double, _
                            dblAdd_Coll As Double, strUS_OL As String, dblNOM_CR As Double, _
                            dblIRR As Double, arrDCF_Spread_Range_1(), intRepricing_Term As Integer, _
                            intInterest_Type_num As Integer, arrPD_Matrix(), dblFinal_PD As Double, dblFinal_PD2 As Double, _
                            strBasel_Type As String, dblManual_LGD As Double, strRV_Balloon As String, _
                            dblcontracted_RV As Double, dblEC_RVR As Double, dblNAF As Double, _
                            dblEC_HC As Double, dblEC_OPR As Double, dblScaling_Factor As Double, _
                            dblHurdle_Rate As Double, dblFundingR As Double, dblSpread As Double, _
                            dblNIBL As Double, dblRV_Enhancements As Double, dblDeal_Rate As Double, _
                            dblManual_MFR_Interest As Double, strInterest_Type As String, _
                            dblManual_MFR_Spread As Double, dblEC_CntryR As Double, _
                            strRORACTargetCase As String, dblAdd_Coll2 As Double, strAdd_Coll_Type As String, arrResults() As typResults) As Variant()


Dim i As Long
Dim j As Long
Dim k As Long
Dim arrCalculation_Generation(0 To 1000, 0 To 39)
Dim dateCash_Flow_Date As Date
Dim dateCash_Flow_Date_Current_Period As Date
Dim dateCash_Flow_Date_Pre_Period As Date
Dim dateFirst_Instalment_Date As Date
Dim arrCalculation_Results(0 To 121)
Dim dblAnnual_PD As Double
Dim dblAnnual_PD2 As Double
Dim dblLGD As Double
Dim dblLGDd As Double

Dim cal_PD_MR As Double
Dim dblAverage_Unsecured_Exposure As Double
Dim dblPV_NIBL_Advantage As Double
Dim dblPV_RV_enhancements As Double
Dim dblPV_EC_CR As Double
Dim dblPV_EC_MR As Double
Dim dblEC_PV_RVR As Double
Dim dblPV_EC_HCR As Double
Dim dblPV_EC_OOR As Double
Dim dblPV_dblEC_MR As Double
Dim dblPV_dblEC_CntryR As Double
Dim dblPV_Cost_of_EC As Double
Dim dblPV_Capital_Advantage As Double
Dim dblPV_Cost_of_Credit_Risk As Double
Dim dblPV_EC As Double
Dim intLoopTo As Integer





Dim wksIndex As Worksheet

Set wksIndex = Sheets("Index")
dblPV_Outstanding = 0


'For i = 0 To UBound(arrCF())
'    Debug.Print lContract.LiqRunoff(i + 1).NBV
'Next i



If dateFirst_Instalment_Date_Input = #12:00:00 AM# Or _
dateFirst_Instalment_Date_Input < DateAdd("m", intPayment_Frequency, datePayout_Date + 1) - 1 Then
    dateFirst_Instalment_Date = DateAdd("m", intPayment_Frequency, datePayout_Date + 1) - 1
Else
    dateFirst_Instalment_Date = dateFirst_Instalment_Date_Input
End If

'Test:
j = 0

If [Manual_CF_Flag] = 1 Then
intLoopTo = intArray_Limit
Else
intLoopTo = intArray_Limit / intPayment_Frequency
End If

For i = 0 To intLoopTo
'--------------------------------------------------------------------------------------------------
    '#2 Cash Flow Date
        arrCalculation_Generation(j, 2) = arrCash_Flow_Generation(i, 0)
    '#2 Cash Flow Date
'--------------------------------------------------------------------------------------------------
    '#0 Period Counter excl Grace Period
    arrCalculation_Generation(j, 0) = arrCash_Flow_Generation(i, 1)
    '#0 Period Counter excl Grace Period
'--------------------------------------------------------------------------------------------------
    '#1 Period Counter incl Grace Period
    arrCalculation_Generation(j, 1) = arrCash_Flow_Generation(i, 2)
    '#1 Period Counter incl Grace Period
'--------------------------------------------------------------------------------------------------
    '#17 Discount Factors Interest Carsta
    arrCalculation_Generation(j, 17) = lContract.LiqRunoff(j + 1).DCF
    '#17 Discount Factors Interest Carsta
'--------------------------------------------------------------------------------------------------
    '#19 DCF Interest * time
    If i = 0 Then
        arrCalculation_Generation(j, 19) = 0
    Else
        dateCash_Flow_Date_Pre_Period = arrCalculation_Generation(j - 1, 2)
        dateCash_Flow_Date_Current_Period = arrCalculation_Generation(j, 2)
        
        arrCalculation_Generation(j, 19) = arrCalculation_Generation(j, 17) * _
        ((arrCalculation_Generation(j, 0) - arrCalculation_Generation(j - 1, 0)) / 12)
        'fct_DiffYears(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period, "Act/Act")
    End If
    '#19 DCF Interest * time
'--------------------------------------------------------------------------------------------------
    '#15 Credit Term Runoff
    If lContract.LiqRunoff(j + 1).NBV > 0 Or arrResults(i).CreditRunOff > 0 Then
        If intRepricing_Term < intCredit_Term And [Interest_Type] <> "Fix" Then
        arrCalculation_Generation(j, 15) = arrResults(i).CreditRunOff
        Else
        arrCalculation_Generation(j, 15) = lContract.LiqRunoff(j + 1).NBV
        End If
    Else
        arrCalculation_Generation(j, 15) = 0
    End If
    '#15 Credit Term Runoff
'--------------------------------------------------------------------------------------------------
    '#16 Discount Factors Interest
    arrCalculation_Generation(j, 16) = 0
    '#16 Discount Factors Interest
'--------------------------------------------------------------------------------------------------
    '#21 Outstanding Amount Final * DCF interest * time
    If i = 0 Then
        arrCalculation_Generation(j, 21) = 0
    Else
        arrCalculation_Generation(j, 21) = arrCalculation_Generation(j - 1, 15) * arrCalculation_Generation(j, 19)
    End If
    '#21 Outstanding Amount Final * DCF interest * time
'--------------------------------------------------------------------------------------------------
    '#23  Annual PD
    If i = 0 Then
        arrCalculation_Generation(j, 23) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrCalculation_Generation(j, 0) = intCredit_Term Then
                arrCalculation_Generation(j, 23) = 0
            Else
                For k = LBound(arrPD_Matrix, 1) To UBound(arrPD_Matrix, 1)
                    If arrPD_Matrix(k, 0) = arrCalculation_Generation(j, 0) Then
                        arrCalculation_Generation(j, 23) = arrPD_Matrix(k, 1)
                        Exit For
                    End If
                Next k
            End If
        Else
            If arrCalculation_Generation(j, 0) > intCredit_Term Then
                arrCalculation_Generation(j, 23) = 0
            Else
                For k = LBound(arrPD_Matrix, 1) To UBound(arrPD_Matrix, 1)
                    If arrPD_Matrix(k, 0) = arrCalculation_Generation(j, 0) Then
                        arrCalculation_Generation(j, 23) = arrPD_Matrix(k, 1)
                        Exit For
                    End If
                Next k
            End If
        End If
    End If
    '#23  Annual PD
j = j + 1
Next i


'--------------------------------------------------------------------------------------------------
j = 0
For i = 0 To intLoopTo
   dblPV_Outstanding = dblPV_Outstanding + arrCalculation_Generation(j, 21)
j = j + 1
Next i

j = 1
For i = 1 To intLoopTo
    dblAverage_Unsecured_Exposure = dblAverage_Unsecured_Exposure + arrLGD_Generation(j, 17) _
    * arrCalculation_Generation(j, 21)
j = j + 1
Next i

dblAverage_Unsecured_Exposure = dblAverage_Unsecured_Exposure / dblPV_Outstanding
'--------------------------------------------------------------------------------------------------


j = 0
For i = 0 To intLoopTo
    '#24  EC Credit Risk
If wksIndex.Range("OneEC").value = "Y" Then
    
    If strUS_OL = "Yes" Then
        If arrCalculation_Generation(j + 1, 0) = intCredit_Term Or IsEmpty(arrCalculation_Generation(j + 1, 0)) Then
            arrCalculation_Generation(j, 24) = 0
        Else
            If dblMSRP < 0 Or dblFinal_PD = 0 Or arrLGD_Generation(j + 1, 17) = 0 Then
                arrCalculation_Generation(j, 24) = 0
            Else
                dblAnnual_PD = dblFinal_PD / 100
                dblAnnual_PD2 = dblFinal_PD2 / 100
                 'Insert Bank Branch PD_Addon for EC Credit Risk
            If InStr(1, wksIndex.Range("Financial_Product_Type").value, "Bank Branch") And wksIndex.Range("Bank_Branch_PD_Addon_for_EC") > 0 Then
                dblAnnual_PD = wksIndex.Range("Modified_Bank_Branch_PD_for_EC_Calc")
            End If
            
            
                dblLGD = arrLGD_Generation(j + 1, 17)
                dblLGDd = arrLGD_Generation(j + 1, 17) + wksIndex.Range("downturn").value + wksIndex.Range("LGD_MOC").value
            
                If strBasel_Type = "Corporate" Or (strBasel_Type = "Dealer" And dblManual_LGD < 0) Then
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD2, dblLGDd, 1) * arrCalculation_Generation(j, 15)
                        cal_PD_MR = Application.WorksheetFunction.Min(0.9999, dblAnnual_PD2 * (1 + (wksIndex.Range("Shiftfactor").value - 1) * (dblEffective_Maturity - 1)))
                        arrCalculation_Generation(j, 38) = (UL_corp(cal_PD_MR, dblLGDd, 1) + cal_PD_MR * dblLGDd)
                        arrCalculation_Generation(j, 39) = arrCalculation_Generation(j, 38) - (UL_corp(dblAnnual_PD2, dblLGDd, 1) + dblAnnual_PD2 * dblLGDd) * arrCalculation_Generation(j, 15)
'                                                       UL_corp(dblAnnual_PD2, dblLGDd, 1) * arrCalculation_Generation(j, 15)
'Else
'                        arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                ElseIf strBasel_Type = "Dealer" Then
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblLGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                Else
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD2, dblLGDd) * arrCalculation_Generation(j, 15)
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD, dblManual_LGD) * arrCalculation_Generation(j, 15)
'                    End If
                End If
            End If
        End If
    Else
        If arrCalculation_Generation(j + 1, 0) > intCredit_Term Or IsEmpty(arrCalculation_Generation(j + 1, 0)) Then
            arrCalculation_Generation(j, 24) = 0
        Else
            If dblMSRP < 0 Or dblFinal_PD = 0 Or arrLGD_Generation(j + 1, 17) = 0 Then
                arrCalculation_Generation(j, 24) = 0
            Else
            dblAnnual_PD = dblFinal_PD / 100
            dblAnnual_PD2 = dblFinal_PD2 / 100
            'Insert Bank Branch PD_Addon for EC Credit Risk
            If InStr(1, wksIndex.Range("Financial_Product_Type").value, "Bank Branch") And wksIndex.Range("Bank_Branch_PD_Addon_for_EC") > 0 Then
                dblAnnual_PD = wksIndex.Range("Modified_Bank_Branch_PD_for_EC_Calc")
            End If
            If wksIndex.Range("OneEC").value = "Y" Then
                dblLGD = arrLGD_Generation(j + 1, 17)
                dblLGDd = arrLGD_Generation(j + 1, 17) + wksIndex.Range("downturn").value + wksIndex.Range("LGD_MOC").value
            Else
                dblLGD = arrLGD_Generation(j + 1, 17)
                dblLGDd = arrLGD_Generation(j + 1, 17)
            End If
            ' 06112024 excluded from if condidton below: Or (strBasel_Type = "Dealer" And dblManual_LGD < 0)
                If strBasel_Type = "Corporate" Then
'                    If dblManual_LGD < 0 Then
                       arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD2, dblLGDd, 1) * arrCalculation_Generation(j, 15)
                        cal_PD_MR = Application.WorksheetFunction.Min(0.9999, dblAnnual_PD2 * (1 + (wksIndex.Range("Shiftfactor").value - 1) * (dblEffective_Maturity - 1)))
                        'cal_PD_MR = Min(0.9999, dblAnnual_PD2 * (1 + (wksIndex.Range("Shiftfactor").value - 1) * (dblEffective_Maturity - 1)))
                        arrCalculation_Generation(j, 38) = (UL_corp(cal_PD_MR, dblLGDd, 1) + cal_PD_MR * dblLGDd)
                        arrCalculation_Generation(j, 39) = (arrCalculation_Generation(j, 38) - (UL_corp(dblAnnual_PD2, dblLGDd, 1) + dblAnnual_PD2 * dblLGDd)) * arrCalculation_Generation(j, 15)

'                    Else
'                        arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                ElseIf strBasel_Type = "Dealer" Then
'                    If dblManual_LGD < 0 Then
                     arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblLGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
    
                    'Else
'                        arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                Else
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD2, dblLGDd) * arrCalculation_Generation(j, 15)
                        cal_PD_MR = Application.WorksheetFunction.Min(0.9999, dblAnnual_PD2 * (1 + (wksIndex.Range("Shiftfactor").value - 1) * (dblEffective_Maturity - 1)))
                        arrCalculation_Generation(j, 38) = (UL_retail(cal_PD_MR, dblLGDd) + cal_PD_MR * dblLGDd)
                        arrCalculation_Generation(j, 39) = (arrCalculation_Generation(j, 38) - (UL_retail(dblAnnual_PD2, dblLGDd) + dblAnnual_PD2 * dblLGDd)) * arrCalculation_Generation(j, 15)



 '                       arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD, dblLGDd) * arrCalculation_Generation(j, 15)
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD, dblManual_LGD) * arrCalculation_Generation(j, 15)
'                    End If
                End If
            End If
        End If
    End If





Else
    If strUS_OL = "Yes" Then
        If arrCalculation_Generation(j + 1, 0) = intCredit_Term Or IsEmpty(arrCalculation_Generation(j + 1, 0)) Then
            arrCalculation_Generation(j, 24) = 0
        Else
            If dblMSRP < 0 Or dblFinal_PD = 0 Or arrLGD_Generation(j + 1, 17) = 0 Then
                arrCalculation_Generation(j, 24) = 0
            Else
                dblAnnual_PD = dblFinal_PD / 100
                 'Insert Bank Branch PD_Addon for EC Credit Risk
            If InStr(1, wksIndex.Range("Financial_Product_Type").value, "Bank Branch") And wksIndex.Range("Bank_Branch_PD_Addon_for_EC") > 0 Then
                dblAnnual_PD = wksIndex.Range("Modified_Bank_Branch_PD_for_EC_Calc")
            End If
            
            If wksIndex.Range("OneEC").value = "Y" Then
                dblLGD = arrLGD_Generation(j + 1, 17)
                dblLGDd = arrLGD_Generation(j + 1, 17) + wksIndex.Range("downturn").value + wksIndex.Range("MOC").value
            Else
                dblLGD = arrLGD_Generation(j + 1, 17)
                dblLGDd = arrLGD_Generation(j + 1, 17)
            End If
                
                
                
                'dblLGD = arrLGD_Generation(j + 1, 17)  '*dcf
                ' dblLGDd = arrLGD_Generation(j + 1, 17) + wksIndex.Range("downturn").value 'normal LGD*dcf + downturnLgd + moc
                If strBasel_Type = "Corporate" Then
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD, dblLGDd, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                ElseIf strBasel_Type = "Dealer" Then
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblLGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                Else
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD, dblLGDd) * arrCalculation_Generation(j, 15)
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD, dblManual_LGD) * arrCalculation_Generation(j, 15)
'                    End If
                End If
            End If
        End If
    Else
        If arrCalculation_Generation(j + 1, 0) > intCredit_Term Or IsEmpty(arrCalculation_Generation(j + 1, 0)) Then
            arrCalculation_Generation(j, 24) = 0
        Else
            If dblMSRP < 0 Or dblFinal_PD = 0 Or arrLGD_Generation(j + 1, 17) = 0 Then
                arrCalculation_Generation(j, 24) = 0
            Else
            dblAnnual_PD = dblFinal_PD / 100
            'Insert Bank Branch PD_Addon for EC Credit Risk
            If InStr(1, wksIndex.Range("Financial_Product_Type").value, "Bank Branch") And wksIndex.Range("Bank_Branch_PD_Addon_for_EC") > 0 Then
                dblAnnual_PD = wksIndex.Range("Modified_Bank_Branch_PD_for_EC_Calc")
            End If
            If wksIndex.Range("OneEC").value = "Y" Then
                dblLGD = arrLGD_Generation(j + 1, 17)
                dblLGDd = arrLGD_Generation(j + 1, 17) + wksIndex.Range("downturn").value + wksIndex.Range("MOC").value
            Else
                dblLGD = arrLGD_Generation(j + 1, 17)
                dblLGDd = arrLGD_Generation(j + 1, 17)
            End If
            
                If strBasel_Type = "Corporate" Then
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD, dblLGDd, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
                        'Debug.Print arrCalculation_Generation(j, 24)
                        'cal_PD_MR = MIN(0.9999;cal_pd_moc*(1+(SF-1)*(maturity-1)))
                        'cal_Corr_Corporate_MR=(0.12*(1-EXP(-50* cal_PD_MR))/(1-EXP(-50))+0.24*(1-(1-EXP(-50* cal_PD_MR))/(1-EXP(-50))))
                        ' cal_b_MR=(0.11852-0.05478*LOG(cal_PD_MR))**2
                        ' cal_VaR_Corporate_MR = ((probnorm(((1-cal_Corr_Corporate_MR)**-0.5)*probit(cal_PD_MR)+((cal_Corr_Corporate_MR/ (1-cal_Corr_Corporate_MR))**0.5)*probit(0.999)))*cal_lgd_MoC)*(1+(1-2.5)*cal_b_MR)/ (1-1.5*cal_b_MR)
' cal_VaR_MR = cal_VaR_Retail_MR or cal_VaR_Corporate_MR analogue to cal_EC_Credit
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_corp(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                ElseIf strBasel_Type = "Dealer" Then
'                    If dblManual_LGD < 0 Then
                     arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblLGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
    
                    'Else
'                        arrCalculation_Generation(j, 24) = UL_Dealer(dblAnnual_PD, dblManual_LGD, dblEffective_Maturity) * arrCalculation_Generation(j, 15)
'                    End If
                Else
'                    If dblManual_LGD < 0 Then
                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD, dblLGDd) * arrCalculation_Generation(j, 15)
'                    Else
'                        arrCalculation_Generation(j, 24) = UL_retail(dblAnnual_PD, dblManual_LGD) * arrCalculation_Generation(j, 15)
'                    End If
                End If
            End If
        End If
    End If
End If

    '#24  EC Credit Risk
'--------------------------------------------------------------------------------------------------
    '#25 EC Residual Value Risk
    If strUS_OL = "Yes" Then
        If arrCash_Flow_Generation(j + 1, 1) = intCredit_Term Then
            arrCalculation_Generation(j, 25) = 0
        Else
            If strRV_Balloon = "Non-guaranteed RV" And dblcontracted_RV > 0 Then
                If strAdd_Coll_Type = 1 Then
                arrCalculation_Generation(j, 25) = dblEC_RVR * dblcontracted_RV * (1 - (dblAdd_Coll2 / 100))
                Else
                arrCalculation_Generation(j, 25) = dblEC_RVR * dblcontracted_RV
                End If
            Else
                arrCalculation_Generation(j, 25) = 0
            End If
        End If
    Else
        If arrCash_Flow_Generation(j + 1, 1) > intCredit_Term Then
            arrCalculation_Generation(j, 25) = 0
        Else
            If strRV_Balloon = "Non-guaranteed RV" And dblcontracted_RV > 0 Then
                If strAdd_Coll_Type = "4" Then
                arrCalculation_Generation(j, 25) = dblEC_RVR * dblcontracted_RV * (1 - (dblAdd_Coll2 / 100))
                Else
                arrCalculation_Generation(j, 25) = dblEC_RVR * dblcontracted_RV
                End If
            Else
                arrCalculation_Generation(j, 25) = 0
            End If
        End If
    End If
    '#25 EC Residual Value Risk
'--------------------------------------------------------------------------------------------------
    '#26 EC Hard Currency Risk
    If strUS_OL = "Yes" Then
        If arrCash_Flow_Generation(j + 1, 1) = intCredit_Term Then
            arrCalculation_Generation(j, 26) = 0
        Else
            arrCalculation_Generation(j, 26) = dblEC_HC * arrCalculation_Generation(j, 15)
        End If
    Else
        If arrCash_Flow_Generation(j + 1, 1) > intCredit_Term Then
            arrCalculation_Generation(j, 26) = 0
        Else
            arrCalculation_Generation(j, 26) = dblEC_HC * arrCalculation_Generation(j, 15)
        End If
    End If
    '#26 EC Hard Currency Risk
'--------------------------------------------------------------------------------------------------
    '#27 EC Operational
    If strUS_OL = "Yes" Then
        If arrCash_Flow_Generation(j + 1, 1) = intCredit_Term Then
            arrCalculation_Generation(j, 27) = 0
        Else
            arrCalculation_Generation(j, 27) = dblEC_OPR * arrCalculation_Generation(j, 15)
        End If
    Else
        If arrCash_Flow_Generation(j + 1, 1) > intCredit_Term Then
            arrCalculation_Generation(j, 27) = 0
        Else
            arrCalculation_Generation(j, 27) = dblEC_OPR * arrCalculation_Generation(j, 15)
        End If
    End If
    '#27 EC Operational
        
    '--------------------------------------------------------------------------------------------------
   
    '#36/37 EC Country + Addon
        
        If arrCash_Flow_Generation(j + 1, 1) > intCredit_Term Then
            arrCalculation_Generation(j, 36) = 0
        Else
            arrCalculation_Generation(j, 36) = dblEC_CntryR * arrCalculation_Generation(j, 15)
            arrCalculation_Generation(j, 37) = dblScaling_Factor * arrCalculation_Generation(j, 15)
        End If
    '#36/37 EC Country + Addon

    '--------------------------------------------------------------------------------------------------
    '#28 EC total incl scaling
    If strUS_OL = "Yes" Then
        If arrCalculation_Generation(j + 1, 0) = intCredit_Term Then
            arrCalculation_Generation(j, 28) = 0
        Else
            arrCalculation_Generation(j, 28) = (arrCalculation_Generation(j, 24) + arrCalculation_Generation(j, 25) + _
                                        arrCalculation_Generation(j, 26) + arrCalculation_Generation(j, 27) + arrCalculation_Generation(j, 36) + arrCalculation_Generation(j, 37))
        End If
    Else
        If arrCalculation_Generation(j + 1, 0) > intCredit_Term Then
            arrCalculation_Generation(j, 28) = 0
        Else
          '######Country Add on
          arrCalculation_Generation(j, 28) = (arrCalculation_Generation(j, 24) + arrCalculation_Generation(j, 25) + _
                                            arrCalculation_Generation(j, 26) + arrCalculation_Generation(j, 27) + arrCalculation_Generation(j, 36) + arrCalculation_Generation(j, 37))
        End If
    End If
    '#28 EC total incl scaling
'--------------------------------------------------------------------------------------------------
    '#29 Cost of EC
    If i = 0 Then
        arrCalculation_Generation(j, 29) = 0
    Else
        arrCalculation_Generation(j, 29) = (arrCalculation_Generation(j - 1, 28) * dblHurdle_Rate) * (fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360)
    End If
    '#29 Cost of EC
'--------------------------------------------------------------------------------------------------
    '#30 Capital Advantage
    If i = 0 Then
        arrCalculation_Generation(j, 30) = 0
    Else
    If wksIndex.Range("Is_Bank_Branch_deal") And (wksIndex.Range("Country_Short") = "ESP" Or wksIndex.Range("Country_Short") = "GBR" Or wksIndex.Range("Country_Short") = "FRA") Then
    Select Case wksIndex.Range("Deal_Currency").value
        Case Worksheets("I_and_S").Range("C2").value
            arrCalculation_Generation(j, 30) = arrCalculation_Generation(j - 1, 28) * ((Worksheets("I_and_S").Range("Manual_MFR1").value / 100)) * (fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360)
        Case Worksheets("I_and_S").Range("I2").value
            arrCalculation_Generation(j, 30) = arrCalculation_Generation(j - 1, 28) * ((Worksheets("I_and_S").Range("Manual_MFR2").value / 100)) * (fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360)
        Case Worksheets("I_and_S").Range("O2").value
            arrCalculation_Generation(j, 30) = arrCalculation_Generation(j - 1, 28) * ((Worksheets("I_and_S").Range("Manual_MFR3").value / 100)) * (fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360)
    End Select
    Else
        arrCalculation_Generation(j, 30) = arrCalculation_Generation(j - 1, 28) * (dblFundingR + dblSpread) * (fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360)
    End If
     End If
    '#30 Capital Advantage
'--------------------------------------------------------------------------------------------------
    '#31 NIBL
    If i = 0 Then
        arrCalculation_Generation(j, 31) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrCash_Flow_Generation(j, 1) = intCredit_Term Then
                arrCalculation_Generation(j, 31) = 0
            Else
                arrCalculation_Generation(j, 31) = (dblNIBL * arrCalculation_Generation(j - 1, 15) + dblAdd_Coll) * (dblFundingR + dblSpread) * (fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360)
            End If
        Else
            If arrCash_Flow_Generation(j, 1) > intCredit_Term Then
                arrCalculation_Generation(j, 31) = 0
            Else
                arrCalculation_Generation(j, 31) = (dblNIBL * arrCalculation_Generation(j - 1, 15) + dblAdd_Coll) * (dblFundingR + dblSpread) * (fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360)
            End If
        End If
    End If
    '#31 NIBL
'--------------------------------------------------------------------------------------------------
    '#32 EAD
    arrCalculation_Generation(j, 32) = arrLGD_Generation(j, 5)
    '#32 EAD
'--------------------------------------------------------------------------------------------------
  '#33 Monthly PDs
    If i = 0 Then
        arrCalculation_Generation(j, 33) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrCash_Flow_Generation(j, 1) = intCredit_Term Then
                arrCalculation_Generation(j, 33) = 0
            Else
                For k = LBound(arrPD_Matrix, 1) To UBound(arrPD_Matrix, 1)
                    If arrPD_Matrix(k, LBound(arrPD_Matrix, 2)) = arrCash_Flow_Generation(j, 1) Then
                        arrCalculation_Generation(j, 33) = (dblFinal_PD / 1200) * (arrCalculation_Generation(j, 0) - arrCalculation_Generation(j - 1, 0))
                        Exit For
                    End If
                Next k
            End If
        Else
            If arrCash_Flow_Generation(j, 1) > intCredit_Term Then
                arrCalculation_Generation(j, 33) = 0
            Else
                 If InStr(1, wksIndex.Range("Financial_Product_Type").value, "Bank Branch") And wksIndex.Range("Bank_Branch_PD_Addon_for_CoR").value > 0 Then
                    arrCalculation_Generation(j, 33) = (wksIndex.Range("Modified_Bank_Branch_PD_for_CoR") / 1200) * (arrCalculation_Generation(j, 0) - arrCalculation_Generation(j - 1, 0))
                Else
                    arrCalculation_Generation(j, 33) = (dblFinal_PD / 1200) * (arrCalculation_Generation(j, 0) - arrCalculation_Generation(j - 1, 0))
                End If
            End If
         End If
    End If
    '#33 Monthly PDs
'--------------------------------------------------------------------------------------------------
    '#34 Cost of Credit Risk
    If i = 0 Then
        arrCalculation_Generation(j, 34) = 0
    Else
        If i < 2 And intPayment_Frequency = 1 And dblManual_LGD < 0 Then
        arrCalculation_Generation(j, 34) = 0
        Else
        If dblManual_LGD < 0 Then
            arrCalculation_Generation(j, 34) = arrCalculation_Generation(j, 32) * arrLGD_Generation(j, 17) * arrCalculation_Generation(j, 33)
        Else
            arrCalculation_Generation(j, 34) = arrCalculation_Generation(j - 1, 15) * arrLGD_Generation(j, 17) * arrCalculation_Generation(j, 33)
        End If
         End If
    End If
    '#34 Cost of Credit Risk
'--------------------------------------------------------------------------------------------------
    '#35 Cost of RV risk (RV adjustment)
    If i = 0 Then
        arrCalculation_Generation(j, 34) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrCash_Flow_Generation(j, 1) > intCredit_Term Then
                arrCalculation_Generation(j, 35) = dblRV_Enhancements
            Else
                arrCalculation_Generation(j, 35) = 0
            End If
        Else
            If arrCash_Flow_Generation(j, 1) = intCredit_Term Then
                arrCalculation_Generation(j, 35) = dblRV_Enhancements
            Else
                arrCalculation_Generation(j, 35) = 0
            End If
        End If
    End If
    '#35 Cost of RV risk (RV adjustment)
j = j + 1
Next i




j = 0
For i = 0 To intLoopTo
    dblPV_Capital_Advantage = dblPV_Capital_Advantage + arrCalculation_Generation(j, 17) _
    * arrCalculation_Generation(j, 30)
    
    dblPV_NIBL_Advantage = dblPV_NIBL_Advantage + arrCalculation_Generation(j, 17) _
    * arrCalculation_Generation(j, 31)
    
    dblPV_Cost_of_Credit_Risk = dblPV_Cost_of_Credit_Risk + arrCalculation_Generation(j, 17) _
    * arrCalculation_Generation(j, 34)
    
    dblPV_RV_enhancements = dblPV_RV_enhancements + arrCalculation_Generation(j, 17) _
    * arrCalculation_Generation(j, 35)
    
    dblPV_EC_CR = dblPV_EC_CR + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 24)
    
    dblEC_PV_RVR = dblEC_PV_RVR + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 25)
    
    dblPV_EC_HCR = dblPV_EC_HCR + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 26)
    
    dblPV_EC_OOR = dblPV_EC_OOR + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 27)
    '######Country Add on
    dblPV_dblEC_CntryR = dblPV_dblEC_CntryR + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 36)
    
    dblPV_EC = dblPV_EC + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 28) + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 39)
    
    dblPV_Cost_of_EC = dblPV_Cost_of_EC + arrCalculation_Generation(j, 17) _
    * arrCalculation_Generation(j, 29)
    
    dblPV_dblEC_MR = dblPV_dblEC_MR + arrCalculation_Generation(j + 1, 19) _
    * arrCalculation_Generation(j, 39)
    
    
j = j + 1
Next i

If strRORACTargetCase = "No" And strLast = "" Then

wksIndex.Range("Mdl_Funding_Adjustment") = dblPV_Capital_Advantage / dblPV_Outstanding
wksIndex.Range("Mdl_NIBL_Adjustment") = dblPV_NIBL_Advantage / dblPV_Outstanding
wksIndex.Range("Mdl_CoCR") = dblPV_Cost_of_Credit_Risk / dblPV_Outstanding

If dblRV_Enhancements <> 0 Then
    wksIndex.Range("Mdl_CoRVR") = dblPV_RV_enhancements / dblPV_Outstanding
Else
    wksIndex.Range("Mdl_CoRVR") = 0
End If

wksIndex.Range("Mdl_EC_RV_Risk") = dblEC_PV_RVR / dblPV_Outstanding
wksIndex.Range("Mdl_EC_HC_Risk") = dblPV_EC_HCR / dblPV_Outstanding
wksIndex.Range("Mdl_EC_Country_Risk") = dblPV_dblEC_CntryR / dblPV_Outstanding
wksIndex.Range("Mdl_EC_Operational") = dblPV_EC_OOR / dblPV_Outstanding
wksIndex.Range("Mdl_Cost_of_Equity") = dblPV_Cost_of_EC / dblPV_Outstanding
wksIndex.Range("Mdl_Average_LGD") = dblAverage_Unsecured_Exposure
wksIndex.Range("mdl_PV_Outstanding") = dblPV_Outstanding
wksIndex.Range("Mdl_EC_MR_Risk") = dblPV_dblEC_MR / dblPV_Outstanding

If wksIndex.Range("Is_Bank_Branch_deal") And wksIndex.Range("DefaultedClient") Then
    wksIndex.Range("Mdl_EC_Credit_Risk") = 0.03
    wksIndex.Range("Mdl_EC") = wksIndex.Range("Mdl_EC_Credit_Risk") + wksIndex.Range("Mdl_EC_Operational") + wksIndex.Range("Mdl_EC_Country_Risk") + wksIndex.Range("Mdl_EC_MR_Risk") + wksIndex.Range("Scaling_Factor")
Else
    wksIndex.Range("Mdl_EC_Credit_Risk") = dblPV_EC_CR / dblPV_Outstanding
    wksIndex.Range("Mdl_EC") = dblPV_EC / dblPV_Outstanding
End If

End If

'    dblStandardCost_RVR = 0
'    If dblRV_Enhancements > 0 Then
'    dblStandardCost_RVR = (dblPV_RV_enhancements / dblPV_Outstanding)
'    End If
If wksIndex.Range("Is_Bank_Branch_deal") And (wksIndex.Range("Country_Short") = "ESP" Or wksIndex.Range("Country_Short") = "GBR" Or wksIndex.Range("Country_Short") = "FRA") Then
    Select Case wksIndex.Range("Deal_Currency").value
        Case Worksheets("I_and_S").Range("C2").value
            dblAct_RORAC = (dblDeal_Rate - (Worksheets("I_and_S").Range("Manual_MFR1").value / 100) + ((dblPV_Capital_Advantage + dblPV_NIBL_Advantage) / dblPV_Outstanding) - (dblPV_Cost_of_Credit_Risk / dblPV_Outstanding) - (dblPV_RV_enhancements / dblPV_Outstanding) - [OPX] + [IDC_periodic]) / (dblPV_EC / dblPV_Outstanding)
        Case Worksheets("I_and_S").Range("I2").value
            dblAct_RORAC = (dblDeal_Rate - (Worksheets("I_and_S").Range("Manual_MFR2").value / 100) + ((dblPV_Capital_Advantage + dblPV_NIBL_Advantage) / dblPV_Outstanding) - (dblPV_Cost_of_Credit_Risk / dblPV_Outstanding) - (dblPV_RV_enhancements / dblPV_Outstanding) - [OPX] + [IDC_periodic]) / (dblPV_EC / dblPV_Outstanding)
        Case Worksheets("I_and_S").Range("O2").value
            dblAct_RORAC = (dblDeal_Rate - (Worksheets("I_and_S").Range("Manual_MFR3").value / 100) + ((dblPV_Capital_Advantage + dblPV_NIBL_Advantage) / dblPV_Outstanding) - (dblPV_Cost_of_Credit_Risk / dblPV_Outstanding) - (dblPV_RV_enhancements / dblPV_Outstanding) - [OPX] + [IDC_periodic]) / (dblPV_EC / dblPV_Outstanding)
    End Select
   
Else
    dblAct_RORAC = (dblDeal_Rate - dblFundingR - dblSpread + ((dblPV_Capital_Advantage + dblPV_NIBL_Advantage) / dblPV_Outstanding) - (dblPV_Cost_of_Credit_Risk / dblPV_Outstanding) - (dblPV_RV_enhancements / dblPV_Outstanding) - [OPX] + [IDC_periodic]) / (dblPV_EC / dblPV_Outstanding)
End If
If wksIndex.Range("Is_Bank_Branch_deal") And wksIndex.Range("DefaultedClient") Then
    dblAct_EbiT = dblAct_RORAC * wksIndex.Range("Mdl_EC")
    dblCurrent_EC = wksIndex.Range("Mdl_EC")
Else
    dblAct_EbiT = dblAct_RORAC * (dblPV_EC / dblPV_Outstanding)
    dblCurrent_EC = dblPV_EC / dblPV_Outstanding
End If

'Debug.Print "--Calculation---------"
'Debug.Print 0; 1; 2; 3; 4; 5; 6; 7; 8; 9; 10; 11; 12; 13; 14; 15; 16; 17; 18; 19; 20; 21; 22; 23; 24; 25; 26; 27; 28; 29; 30; 31; 32; 33; 34; 35
'For i = 0 To j - 1
'Debug.Print arrCalculation_Generation(i, 0) & ";" & arrCalculation_Generation(i, 1) & ";" & _
'arrCalculation_Generation(i, 2) & ";" & arrCalculation_Generation(i, 3) & ";" & _
'arrCalculation_Generation(i, 4) & ";" & arrCalculation_Generation(i, 5) & ";" & _
'arrCalculation_Generation(i, 6) & ";" & arrCalculation_Generation(i, 7) & ";" & _
'arrCalculation_Generation(i, 8) & ";" & arrCalculation_Generation(i, 9) & ";" & _
'arrCalculation_Generation(i, 10) & ";" & arrCalculation_Generation(i, 11) & ";" & _
'arrCalculation_Generation(i, 12) & ";" & arrCalculation_Generation(i, 13) & ";" & _
'arrCalculation_Generation(i, 14) & ";" & arrCalculation_Generation(i, 15) & ";" & _
'arrCalculation_Generation(i, 16) & ";" & arrCalculation_Generation(i, 17) & ";" & _
'arrCalculation_Generation(i, 18) & ";" & arrCalculation_Generation(i, 19) & ";" & _
'arrCalculation_Generation(i, 20) & ";" & arrCalculation_Generation(i, 21) & ";" & _
'arrCalculation_Generation(i, 22) & ";" & arrCalculation_Generation(i, 23) & ";" & _
'arrCalculation_Generation(i, 24) & ";" & arrCalculation_Generation(i, 25) & ";" & _
'arrCalculation_Generation(i, 26) & ";" & arrCalculation_Generation(i, 27) & ";" & _
'arrCalculation_Generation(i, 28) & ";" & arrCalculation_Generation(i, 29) & ";" & _
'arrCalculation_Generation(i, 30) & ";" & arrCalculation_Generation(i, 31) & ";" & _
'arrCalculation_Generation(i, 32) & ";" & arrCalculation_Generation(i, 33) & ";" & _
'arrCalculation_Generation(i, 34) & ";" & arrCalculation_Generation(i, 35)
'Next i

'--Calculation Results to Excel Sheet Calculation------------------------------------------------
'Call fctClear_Range(Sheets("Calculation").Range("Mdl_Calculation"))
'If strRORACTargetCase = "No" And strLast = "" Then
'For i = 0 To UBound(arrCF)
'    For j = 1 To 35
'    Sheets("Calculation").Range("Mdl_Calculation")(i + 1, j) = arrCalculation_Generation(i, j)
'Next j
'Next i
'End If
'--Calculation Results to Excel Sheet Calculation------------------------------------------------

'-----Output

'--DCF * time to Excel Sheet Index------------------------------------------------

Sheets("Index").Range("mdl_discount_rates").ClearContents
For i = 1 To UBound(arrCF)
Sheets("Index").Range("mdl_discount_rates")(i + 1, 1) = lContract.LiqRunoff(i + 1).DCF
Next i


For i = 0 To j - 1
arrCalculation_Results(i) = arrCalculation_Generation(i, 17)

Next i
fctCalculation_Generation = arrCalculation_Results()
'-----Output
'-----Output
End Function







--- Macro File: DieseArbeitsmappe.cls ---
Attribute VB_Name = "DieseArbeitsmappe"
Attribute VB_Base = "0{00020819-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True

Private Sub Workbook_BeforeClose(Cancel As Boolean)
    Worksheets("New Input Mask").CommandButton11.Visible = True
    
    If Worksheets("Data_Entities").Visible <> xlVeryHidden Then
         Worksheets("Data_Entities").Visible = xlVeryHidden
    End If
    
    If Worksheets("Index").Visible <> xlVeryHidden Then
         Worksheets("Index").Visible = xlVeryHidden
    End If
    
End Sub

Private Sub Workbook_Open()

'Check if tool is already initialized. If not then all sheets except "Initialize" will be hidden
If Worksheets("Index").[Initialized] = "Yes" Then
    Worksheets("Data_Entities").Visible = xlVeryHidden
    Worksheets("Index").Visible = xlVeryHidden
    If [Country_Short] = "USA" Then
        On Error Resume Next
        Worksheets("New Input Mask").CommandButton11.Visible = False
    Else
        On Error Resume Next
        Worksheets("New Input Mask").CommandButton11.Visible = False
    End If
    ActiveWindow.DisplayHeadings = False
    Worksheets("Portfolio").Protect Password:="Blattschutz"
    Worksheets("New Input Mask").Activate
    Worksheets("New Input Mask").ScrollArea = "A1:Z100"
    Application.EnableEvents = True
    ActiveWindow.DisplayWorkbookTabs = False
    Application.StatusBar = ""
    Call protectInput
Else
    Worksheets("Data_Entities").Visible = xlVeryHidden
    Worksheets("Index").Visible = xlVeryHidden
    ActiveWindow.DisplayHeadings = False
    Worksheets("Portfolio").Protect Password:="Blattschutz"
    Worksheets("New Input Mask").Visible = xlVeryHidden
    Worksheets("Portfolio").Visible = xlVeryHidden
    Worksheets("BOM Deals").Visible = xlVeryHidden
    Worksheets("Cash Flow Analysis").Visible = xlVeryHidden
    Worksheets("Local_Sheet").Visible = xlVeryHidden
    Worksheets("I_and_S").Visible = xlVeryHidden
    Worksheets("Initialize").Visible = True
    Worksheets("Manual_Cash_Flows").Visible = xlVeryHidden
    Application.EnableEvents = True
    ActiveWindow.DisplayWorkbookTabs = False
    Call protectInput
    Worksheets("Initialize").Visible = True
    Application.StatusBar = ""
End If

End Sub

Private Sub Workbook_PivotTableCloseConnection(ByVal Target As PivotTable)

End Sub


--- Macro File: frm_LGDDev.frm ---
Attribute VB_Name = "frm_LGDDev"
Attribute VB_Base = "0{C3B04C97-73CC-46F9-8389-72647A3FF399}{8B40D009-6237-4D55-BB33-BC3EE9831530}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
'Sub to close form
Private Sub CommandButton2_Click()
Unload Me
End Sub

'sub to print form
Private Sub CommandButton3_Click()
    Call prcPrintForm(Me)
End Sub

'Create chart and display on form
Private Sub UserForm_activate()

On Error GoTo Fehler:

Dim oldzoom As Integer

Application.ScreenUpdating = False
Worksheets("Index").Activate
Worksheets("Index").Range("E1").Select
oldzoom = ActiveWindow.Zoom
ActiveWindow.Zoom = 100

'Chart will be exported from index sheet into gif-file and this file will be loaded on form
filePCSChart = "chartPCS.gif"
Worksheets("Index").ChartObjects("LGDChart").Activate
ActiveChart.ChartArea.Select
With ActiveChart
        .Parent.Width = frm_LGDDev.Image1.Width
        .Parent.Height = frm_LGDDev.Image1.Height
End With
ActiveChart.ChartArea.Copy
ActiveWindow.Visible = False
'Export chart into gif-file
ActiveChart.Export filePCSChart
frm_LGDDev.Image1.Picture = LoadPicture(filePCSChart)

'Delete of temp-gif file
Kill filePCSChart

ActiveWindow.Zoom = oldzoom

Worksheets("New Input Mask").Activate
Application.ScreenUpdating = True
Exit Sub

Fehler:

Worksheets("New Input Mask").Activate
Application.ScreenUpdating = True
MsgBox "An error occured. Chart can not be shown."
End Sub


--- Macro File: Frm_Target_Red.frm ---
Attribute VB_Name = "Frm_Target_Red"
Attribute VB_Base = "0{0C766A63-4714-43C0-89DD-98C03797DBF2}{307FEB7B-4D21-4958-BE28-87A43A6CD4A7}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False


Private Sub UserForm_activate()
Label2.Caption = "Required " & Worksheets("New Input Mask").ComboBox3.value
CommandButton1.Caption = "Calculate " & Worksheets("New Input Mask").ComboBox3.value
End Sub

Private Sub CommandButton1_Click()

Dim value As Double


If (TextBox1.value <> "" And Not IsNumeric(TextBox1.value)) Or (TextBox1.value = "") Then
        MsgBox "Please enter a correct Percentage Value"
        Exit Sub
End If

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

If Application.UseSystemSeparators = True Then
    value = CDbl(TextBox1.value)
Else
    value = CDbl(Replace(TextBox1.value, strApp_Dec_Separator, strOS_Dec_Separator))
End If

[Target_RORAC] = value / 100
[Target_RORAC_Case] = "Yes"
[Target_Type] = "2"
Call unprotectInput

Call prcStartCalculation

If Application.UseSystemSeparators = True Then
    TextBox2.value = WorksheetFunction.Round([Target_Rate] * 100, 2)
Else
    TextBox2.value = Replace(WorksheetFunction.Round([Target_Rate] * 100, 2), strOS_Dec_Separator, strApp_Dec_Separator)
End If

Label5.Caption = [Target_Rate] * 100
[Target_RORAC_Case] = "No"

Call protectInput

End Sub

Private Sub CommandButton2_Click()

If (TextBox2.value = "") Then
        MsgBox "Please Calculate a Target Customer Rate"
        Exit Sub
End If

Worksheets("New Input Mask").Range("H54").value = CDbl(Label5.Caption)
[Nom_CR_MCF] = CDbl(Label5.Caption)
'Worksheets("New Input Mask").ComboBox3.Value = "Customer Rate"

Unload Me

Call unprotectInput

Call prcStartCalculation

[Target_RORAC_Case] = "No"
Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "Target RORAC was successfully calculated"
End Sub

Private Sub CommandButton3_Click()
[Target_RORAC_Case] = "No"
Unload Me
End Sub



--- Macro File: mdlLGD_Functions.bas ---
Attribute VB_Name = "mdlLGD_Functions"
Option Explicit

'#2
Public Function fctCash_Flow_DateII(dateCash_Flow_Date As Date, _
                                    intEAD_Adjustment_Factor As Integer, _
                                    intPayment_Frequency As Integer, _
                                    datePayout_Date As Date, _
                                    j As Long) As Date

'fctCash_Flow_DateII = DateAdd("m", intEAD_Adjustment_Factor, dateCash_Flow_Date + 1) - 1
If arrLGD_Generation(j, 0) <= -intEAD_Adjustment_Factor Then
        fctCash_Flow_DateII = datePayout_Date
    Else
        fctCash_Flow_DateII = arrLGD_Generation(Application.WorksheetFunction.Max(j - Application.WorksheetFunction.Max(1, -intEAD_Adjustment_Factor / intPayment_Frequency), 0), 1)
    End If
End Function

'#3
Public Function fctAmortization_IDC_Subsidies(dblAmortization_IDC_Subsidies_Period_0 As Double, _
                                              dblAmortization_IDC_Subsidies_Pre_Period As Double, _
                                              intPeriod_Counter_excl_Grace_Period_Pre_Period As Integer, _
                                              intPeriod_Counter_excl_Grace_Period_Current_Period As Integer, _
                                              intCredit_Term As Integer, _
                                              strUS_OL As String, _
                                              intPayment_Frequency As Integer) As Double
                                              
If strUS_OL = "Yes" Then
    If intPeriod_Counter_excl_Grace_Period_Current_Period = intCredit_Term Then
        fctAmortization_IDC_Subsidies = dblAmortization_IDC_Subsidies_Pre_Period
    Else
        fctAmortization_IDC_Subsidies = dblAmortization_IDC_Subsidies_Pre_Period - dblAmortization_IDC_Subsidies_Period_0 / _
                                        (intCredit_Term - intPayment_Frequency) * (intPeriod_Counter_excl_Grace_Period_Current_Period - _
                                        intPeriod_Counter_excl_Grace_Period_Pre_Period)
    End If
Else
    If intPeriod_Counter_excl_Grace_Period_Current_Period > intCredit_Term Then
        fctAmortization_IDC_Subsidies = dblAmortization_IDC_Subsidies_Pre_Period
    Else
        fctAmortization_IDC_Subsidies = dblAmortization_IDC_Subsidies_Pre_Period - dblAmortization_IDC_Subsidies_Period_0 / _
                                        intCredit_Term * (intPeriod_Counter_excl_Grace_Period_Current_Period - _
                                        intPeriod_Counter_excl_Grace_Period_Pre_Period)
    End If
End If
End Function


--- Macro File: Tabelle8.cls ---
Attribute VB_Name = "Tabelle8"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "Label1, 443, 3, MSForms, Label"
Attribute VB_Control = "CommandButton1, 8, 4, MSForms, CommandButton"
Attribute VB_Control = "CommandButton2, 9, 5, MSForms, CommandButton"
Attribute VB_Control = "CommandButton3, 10, 6, MSForms, CommandButton"
Attribute VB_Control = "CommandButton4, 11, 7, MSForms, CommandButton"
Attribute VB_Control = "ComboBox1, 29, 8, MSForms, ComboBox"
Attribute VB_Control = "ComboBox2, 31, 9, MSForms, ComboBox"
Attribute VB_Control = "ComboBox3, 44, 10, MSForms, ComboBox"
Attribute VB_Control = "CommandButton5, 56, 11, MSForms, CommandButton"
Attribute VB_Control = "ComboBox4, 67, 12, MSForms, ComboBox"
Attribute VB_Control = "CommandButton9, 170, 13, MSForms, CommandButton"
Attribute VB_Control = "CommandButton10, 233, 14, MSForms, CommandButton"
Attribute VB_Control = "CommandButton11, 243, 15, MSForms, CommandButton"
Attribute VB_Control = "CommandButton12, 291, 16, MSForms, CommandButton"
Attribute VB_Control = "CommandButton14, 298, 17, MSForms, CommandButton"
Attribute VB_Control = "SpinButton1, 336, 18, MSForms, SpinButton"
Attribute VB_Control = "CommandButton15, 396, 19, MSForms, CommandButton"
Attribute VB_Control = "CommandButton16, 398, 20, MSForms, CommandButton"
Attribute VB_Control = "CommandButton17, 421, 21, MSForms, CommandButton"
Attribute VB_Control = "ComboBox5, 426, 22, MSForms, ComboBox"
Attribute VB_Control = "CommandButton22, 436, 23, MSForms, CommandButton"
Attribute VB_Control = "CommandButton23, 437, 24, MSForms, CommandButton"
Attribute VB_Control = "CommandButton18, 438, 25, MSForms, CommandButton"
Attribute VB_Control = "CommandButton19, 471, 26, MSForms, CommandButton"
Attribute VB_Control = "CommandButton20, 473, 27, MSForms, CommandButton"
Attribute VB_Control = "ComboBox6, 496, 28, MSForms, ComboBox"
Attribute VB_Control = "ComboBox7, 517, 29, MSForms, ComboBox"
Attribute VB_Control = "CommandButton7, 537, 30, MSForms, CommandButton"
'If unit for downpayment is in % then the value has to be below 100
Private Sub ComboBox1_Change()
If Left(ComboBox1.text, 1) = "%" Then
    If Worksheets("New Input Mask").Range("E33").value > 100 Then
        MsgBox "Please enter a valid percentage value"
    End If
End If
            
End Sub

'If unit for RV is in % then the value has to be below 100
Private Sub ComboBox2_Change()
If Left(ComboBox2.text, 1) = "%" Then
    If Worksheets("New Input Mask").Range("E45").value > 100 Then
        MsgBox "Please enter a valid percentage value"
    End If
End If
End Sub



'sub to open a form for the discount
Private Sub CommandButton12_Click()
If Worksheets("New Input Mask").Range("e17") = "" Then
    MsgBox ("Please enter a List Price first")
    Exit Sub
Else
    Frm_DiscLP.Show
End If
End Sub

'sub to open the calender to enter a calculation date
Private Sub CommandButton13_Click()
Frm_Calc_Date.Show
End Sub

'sub to open a form for the selection of a benchmark rate
Private Sub CommandButton14_Click()
Frm_Benchmark.Show
End Sub

'sub to open a form to insert a comment
Private Sub CommandButton15_Click()
Frm_Comments.Show
End Sub

'sub export the parameters of the current selected entity. For Mex this function is deactivated due to an instruction from US
'for the export the temporary sheet "Data entities2" is used
Private Sub CommandButton16_Click()

If [Country_Short] = "MEX" Then
    Exit Sub
End If
On Error GoTo Fehler

'Determine position of selected entity on data entities sheet
intEntity_Row = fct_entity_data_position()

If intEntity_Row = 0 Then
    MsgBox "No Entity Data available"
    Exit Sub
End If

Application.ScreenUpdating = False
Worksheets("Data_Entities").Visible = True

'Activate temp sheet and delete current entries
tmpName = ActiveWorkbook.Name
Worksheets("Data_Entities2").Visible = True
Worksheets("Data_Entities2").Activate
Worksheets("Data_Entities2").Range("A2:BN101").Select
Selection.Clear

'Select Range on Data_Entities that needs to be copied
Worksheets("Data_Entities").Select
Dim eins As String
Dim zwei As String
eins = Replace("A" & Str(intEntity_Row), " ", "")
zwei = Replace("BZ" & Str(intEntity_Row + 99), " ", "")
Worksheets("Data_Entities").Range(eins & ":" & zwei).Select

Selection.Copy

'Paste data on select sheet --> Values and Formats will be copied and then exported into a new file
Worksheets("Data_Entities2").Activate
Worksheets("Data_Entities2").Range("A2").Select
Worksheets("Data_Entities2").Range("A2").PasteSpecial Paste:=xlValues, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
Worksheets("Data_Entities2").Range("A2").PasteSpecial Paste:=xlFormats, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
Worksheets("Data_Entities2").Copy
ActiveWorkbook.Colors = Workbooks(tmpName).Colors

'Save new file --> User needs to enter location and name via dialog
Application.CutCopyMode = False
fname = Application.GetSaveAsFilename(fileFilter:="Excel files (*.xlsx), *.xlsx")
Application.DisplayAlerts = False
If fname <> False Then
ActiveWorkbook.SaveAs Filename:= _
        fname, FileFormat:=xlOpenXMLWorkbook _
        , Password:="", WriteResPassword:="", ReadOnlyRecommended:=False, _
        CreateBackup:=False
End If
Application.DisplayAlerts = True
ActiveWorkbook.Saved = True
ActiveWorkbook.Close 'savechanges:=False
Worksheets("Data_Entities2").Visible = False
Worksheets("Data_Entities").Visible = xlVeryHidden
Worksheets("New Input Mask").Activate
Worksheets("New Input Mask").Range("E8").Select

'Protects Sheet after finishing
Application.Calculate
With Application
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
Application.EnableEvents = True
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("New Input Mask").Protect Password:="Blattschutz"
Exit Sub

'Error handling
Fehler:
MsgBox "An Error occured. Maybe you are not allowed to save the file in the selected folder."
tmpName2 = ActiveWorkbook.Name

'If already a new file was opened and activated it needs to be closed and tool needs to be activated
If tmpName <> tmpName2 Then
    ActiveWorkbook.Saved = True
    ActiveWorkbook.Close
End If

Workbooks(tmpName).Activate
Worksheets("Data_Entities2").Visible = False
Worksheets("Data_Entities").Visible = xlVeryHidden
Worksheets("New Input Mask").Activate
Worksheets("New Input Mask").Range("E8").Select

'Protects Sheet after finishing

With Application
    .Calculate
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
Application.EnableEvents = True
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("New Input Mask").Protect Password:="Blattschutz"
End Sub

'sub to export input mask
'for the export the temporary sheet "New Input Mask2" is used
Private Sub CommandButton17_Click()
On Error GoTo Fehler

Call unprotectInput
Application.EnableEvents = False

tmpName = ActiveWorkbook.Name

'activate "New Input Mask2", delete current entries and paste form input mask
Worksheets("New Input Mask2").Visible = True
Worksheets("New Input Mask2").Activate
Worksheets("New Input Mask2").Range("B2:Y72").Select
Selection.Clear
Worksheets("New Input Mask").Activate
Worksheets("New Input Mask").Range("B2:Y72").Select
Selection.Copy
Worksheets("New Input Mask2").Activate
Worksheets("New Input Mask2").Range("B2").Select
Worksheets("New Input Mask2").Range("B2").PasteSpecial Paste:=xlValues, Operation:=xlNone, SkipBlanks:=False, Transpose:=False
Worksheets("New Input Mask2").Range("B2").PasteSpecial Paste:=xlFormats, Operation:=xlNone, SkipBlanks:=False, Transpose:=False

'------------------------------------------------------------------------------
'Extend new information onto the Deal sheet
Worksheets("New Input Mask2").Range("L52").value = "Customer Instalment:"
Worksheets("New Input Mask2").Range("Q52").value = "EaB:"
Worksheets("New Input Mask2").Range("Q54").value = "RoE:"
Worksheets("New Input Mask2").Range("Q52:Q54").HorizontalAlignment = xlCenter
Worksheets("New Input Mask2").Range("U52").value = "Target RORAC:"
Worksheets("New Input Mask2").Range("U54").value = "Req. Cust. Rate:"

If Worksheets("INDEX").Range("final_PD") = 0 Then
        export_lgd = "PLS: " & Application.Round([Mdl_Average_LGD], 4) * 100 & "%"
    Else
        export_lgd = "PLS: " & Application.Round(([Mdl_CoCR]) / [final_PD], 6) * 10000 & "%"
End If
'bank branch
If Worksheets("INDEX").Range("Country_short") = "GBR" And Worksheets("INDEX").Range("Is_Bank_Branch_deal") Then
        export_lgd = "PLS: " & Application.Round([Mdl_Average_LGD], 4) * 100 & "%"
End If

Worksheets("New Input Mask2").Range("V10").value = export_lgd
Worksheets("New Input Mask2").Range("V10").Interior.ColorIndex = 11
Worksheets("New Input Mask2").Range("V10").Font.ColorIndex = 2
Worksheets("New Input Mask2").Range("V10").Font.Size = "8"
Worksheets("New Input Mask2").Range("V10").HorizontalAlignment = xlCenter
Worksheets("New Input Mask2").Range("V10").Interior.Color = RGB(38, 63, 106)
Worksheets("New Input Mask2").Range("S52").value = (Application.Round(Worksheets("INDEX").Range("Equity_Input_Mask"), 4))
Worksheets("New Input Mask2").Range("S54").value = (Application.Round(Worksheets("INDEX").Range("Roe_Input_Mask"), 4))
Worksheets("New Input Mask2").Range("S52:S54").HorizontalAlignment = xlCenter
Worksheets("New Input Mask2").Range("S52:S54").NumberFormat = "#.#0%"

Worksheets("New Input Mask2").Range("X52").value = (Application.Round(Worksheets("INDEX").Range("L156"), 4))
Worksheets("New Input Mask2").Range("X54").value = (Application.Round(Worksheets("INDEX").Range("M147"), 4))
Worksheets("New Input Mask2").Range("X52").NumberFormat = "#.0%"
Worksheets("New Input Mask2").Range("X54").NumberFormat = "#.#0%"
Worksheets("New Input Mask2").Range("L52:X54").Font.Size = "9"
Worksheets("New Input Mask2").Range("L52:X54").Font.Color = RGB(38, 63, 106)
'------------------------------------------------------------------------------

'copy "New Input Mask2" to another file
Worksheets("New Input Mask2").Copy

'Delete buttons and all other vba elements
ActiveWorkbook.Colors = Workbooks(tmpName).Colors
Dim Bild As Shape
For Each Bild In ActiveSheet.Shapes
    Bild.Delete
Next

'Save new file --> User needs to enter location and name via dialog
ActiveSheet.Rows(1).Hidden = True
Application.CutCopyMode = False
fname = Application.GetSaveAsFilename(fileFilter:="Excel files (*.xlsx), *.xlsx")
Application.DisplayAlerts = False
If fname <> False Then
ActiveWorkbook.SaveAs Filename:= _
        fname, FileFormat:=xlOpenXMLWorkbook _
        , Password:="", WriteResPassword:="", ReadOnlyRecommended:=False, _
        CreateBackup:=False
End If
Application.DisplayAlerts = True
ActiveWorkbook.Saved = True
ActiveWorkbook.Close 'savechanges:=False
Worksheets("New Input Mask2").Visible = False
Worksheets("New Input Mask").Activate
Worksheets("New Input Mask").Range("E8").Select

'Protects Sheet after finishing
Application.Calculate
With Application
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
Application.EnableEvents = True
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("New Input Mask").Protect Password:="Blattschutz"
Exit Sub

'error handling
Fehler:
MsgBox "An Error occured. Maybe you are not allowed to save the file in the selected folder."
tmpName2 = ActiveWorkbook.Name

If tmpName <> tmpName2 Then
    ActiveWorkbook.Saved = True
    ActiveWorkbook.Close
End If

'If already a new file was opened and activated it needs to be closed and tool needs to be activated
Workbooks(tmpName).Activate
Worksheets("New Input Mask2").Visible = False
Worksheets("New Input Mask").Activate
Worksheets("New Input Mask").Range("E8").Select

'Protects Sheet after finishing
With Application
    .Calculate
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
Application.EnableEvents = True
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("New Input Mask").Protect Password:="Blattschutz"
End Sub

'sub to reset all entries
Private Sub CommandButton18_Click()
Dim i  As Integer
Dim lItem As Integer

i = MsgBox("Do you want to reset the Input Values", 1 + vbQuestion)
If i = 2 Then Exit Sub

Call unprotectInput
Application.EnableEvents = False

With Worksheets("New Input Mask")
    
    'exception for France
    If [Country_Short] <> "FRA" Then
        'Reset of Customer Type
        .Range("E8").value = ""
    End If
    
    'Reset of Customer Name
    .Range("L8").value = ""
    
    'Reset of Internal Rating
    .Range("E10").value = ""

    'Reset of Manual PD
    .Range("E12").value = ""
    
    'Reset of List Price
    .Range("E17").value = ""
    
    'Reset of Number of Vehicles
    .Range("L17").value = ""
    
    'Reset of Age of Used Vehicle
    .Range("U17").value = ""
        
    'Reset of Asset Valuation Curve
    .Range("E19").value = ""
        
    'Reset of Additional Collateral
    .Range("L19").value = ""
    
    'Reset of Additional Collateral type
    .Range("U19").value = ""
        
    'Reset of Deal Currency
    .Range("D27").value = Worksheets("INDEX").Range("X86")
    
    'Reset of Additional Collateral Type
    .Range("Q19").value = Worksheets("Index").Range("Aa4").value
        
    'Reset of Financial Product Type
    .Range("e25").value = ""
      
    'Reset of OPEX Segment
    .Range("G27").value = ""
        
    'Reset of Sales Price
    .Range("H29").value = ""
    
    'Reset of Add. Finan. Items
    .Range("H31").value = ""
    
    'Reset of Downpayment
    .Range("E33").value = ""
    
    'Reset of Initial Direct Cost
    .Range("E37").value = ""
    
    'Reset of Subsidies and Fees (upfront)
    .Range("H39").value = ""
    
    'Reset of Commission, Subsidies and Fees (Periodic)
    .Range("H41").value = ""
    
    'Security Deposit
    .Range("H43").value = ""
    
    'Reset of RV
    .Range("E45").value = ""
    
    'Reset of Credit Term
    .Range("H47").value = ""
    
    'exception for France
    If [Country_Short] <> "FRA" Then
        'Reset of Interest Rate Type
        .Range("E49").value = ""
        
        'Reset of Repricing Term
        .Range("H49").value = ""
        
        'Reset of Payment Frequency
        .Range("E52").value = ""
        
        'Reset of Payment Mode
        .Range("H52").value = ""
    End If

    'Reset of Payout Date
    .Range("E54").value = ""
    
    'Reset of Rate Type
     ComboBox3.value = Worksheets("INDEX").Range("R2")
    
    'Reset of Rate Type Value
    .Range("H54").value = ""
    
    'Reset of First Installment Date
    .Range("E58").value = ""
    
    'Reset of Government Flag
    .Range("H58").value = "None"
    
    'Reset of Interest Only Period
    .Range("H60").value = "0"
    
    'Reset of Postponed Balloon
    .Range("H62").value = "No"
    
    'Reset of Extraordinary Payment Date
    .Range("E60").value = ""
    
    'Reset of Extraordinary Payment Amount
    .Range("E62").value = ""
    
    'Reset of Residual Value Adjustment
    .Range("E64").value = ""
    
    'Reset of Amortization Method
    .Range("H64").value = "Amortizing"
    
    'Calculation Date
    .Range("E66").value = ""
    
    'Reset of Non-Interest Bearing Items
    .Range("E68").value = ""
    
    'Reset of Balloon/RV for Last installment
    .Range("E70").value = "does not comprise last installment"
    
    'Reset of Comment1
    .Range("L58").value = ""
    
    'Reset of Comment2
    .Range("L74").value = ""
    
    'Reset of accelerated Payment
    .Range("E60").value = "No"
    .Range("E61").Locked = True
    .Range("E62").Locked = True
    
End With

'exception for france to delete values on local sheet
If [Country_Short] = "FRA" Then
    Worksheets("Local_Sheet").Range("Differe") = "Non"
    Worksheets("Local_Sheet").Range("Day_Differe") = 0
    Worksheets("Local_Sheet").Range("Assurance") = "Non"
    Worksheets("Local_Sheet").Range("Assurance1") = "No Insurance"
    Worksheets("Local_Sheet").Range("Assurance2") = "No Insurance"
    Worksheets("Local_Sheet").Range("Per_Annee") = 0
    Worksheets("Local_Sheet").Range("Duree_Contrat") = 0
    Worksheets("Local_Sheet").Range("Montant_annuel") = 0
    Worksheets("Local_Sheet").Range("perc_dur_contrat") = 0
End If

'In case MCF was activated this will be reseted as well
Worksheets("Index").Range("Manual_CF_Flag") = 0
CommandButton23.Visible = False
CommandButton22.Caption = "Use MCF"
Worksheets("New Input Mask").ComboBox7.Visible = False
Worksheets("Index").Range("Accelerated_Payment_Flag") = 0

Call protectInput
Application.EnableEvents = True
End Sub

'sub to open the form for updating I und S
Private Sub CommandButton19_Click()
If Worksheets("New Input Mask").Range("L5").value = "Please choose an Entity" Then
    MsgBox "Please choose an Entity"
    Exit Sub
End If
Frm_Password_IS.Show
End Sub

'sub to open local sheet. In case of France the RORAC will be calculated before
Private Sub CommandButton20_Click()

If [Country_Short] = "FRA" Then
    Call unprotectInput
    
    Dim zelle As Range
    Dim lItem As Integer
    Dim AnzMonths As Integer
    Dim intLastInst
    Dim bolInp_OK As Boolean
    
    lItem = 0
    
    
    bolInp_OK = fct_checkInput()
    
    If bolInp_OK = False Then
        Call protectInput
        Exit Sub
    End If
    
    
'MessageBox If stored Interest and Spreads older than one week or one month (Exception for Mexico, Spain and Thailand)
If Worksheets("Index").Range("Expiry_Date_IS") < Date And [Country_Short] <> "MEX" Then
    If [Country_Short] = "RUS" Then
        MsgBox ("Stored Interests and Spreads are older than one week (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    ElseIf [Country_Short] = "THA" Then
        MsgBox ("Stored Interests and Spreads are older than two weeks (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    Else
        MsgBox ("Stored Interests and Spreads are older than one month (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    End If
End If

    
    
    'MessageBox If stored Manual Cash Flow is used
    If Worksheets("Index").Range("Manual_CF_Flag") = 1 Then
        MsgBox ("Manual Cash Flow is active and will be considered for calculation!")
    End If
    
    'MessageBox If more than one collateral is added
    If Worksheets("Index").Range("C323").value <> "" Or Worksheets("Index").Range("C324").value <> "" Then
        MsgBox ("More than one additional collateral was added and will be considered for calculation!")
    End If
    
    'MessageBox If stored Acceleraded Payment is used
    If Worksheets("Index").Range("Accelerated_Payment_Flag") = 1 Then
        MsgBox ("Accelerated Payment is active and will be considered for calculation!")
        Worksheets("Index").Range("write_mcf").value = "yes"
        Worksheets("Index").Range("Accelerated_Payment_Flag") = 0
        Call prcStartCalculation
        Worksheets("Index").Range("write_mcf").value = "no"
        Worksheets("Index").Range("Accelerated_Payment_Flag") = 1
        Application.Calculate
    End If
    'Start RORAC-Calculation
    Call prcStartCalculation
    
    Call LGD_button_CalcDate
    
    'Protect Sheet after successfully RORAC Calculation
    Call protectInput
    
    'MsgBox "RORAC was successfully calculated"
    Application.StatusBar = "RORAC was successfully calculated"
End If

'For France depending on the choosen entitiy (fleet, dealer or charterway) a different section of the local sheet is selected and
'scroll areas are set to freeze the relevant section on the local sheet

If [Company] = "(FRA) Mercedes-Benz Financial Services France S.A. (Dealer)" Then
Worksheets("Local_Sheet").Activate
Worksheets("Local_Sheet").Range("A237").Select
Worksheets("Local_Sheet").ScrollArea = "A147:AI280"
ActiveWindow.LargeScroll Down:=-2
    Else
    If [Company] = "(FRA) Mercedes-Benz Charterway" Then
    Worksheets("Local_Sheet").Activate
    Worksheets("Local_Sheet").Range("A336").Select
    Worksheets("Local_Sheet").ScrollArea = "A300:AI355"
    ActiveWindow.LargeScroll Down:=-1
    Else
    Worksheets("Local_Sheet").Activate
    Worksheets("Local_Sheet").Range("A60").Select
    'Worksheets("Local_Sheet").ScrollArea = "A1:AZ120"
    ActiveWindow.LargeScroll Down:=-1
    End If
End If
    
Worksheets("Local_Sheet").Activate

End Sub

'sub to open MCF field and set MCF status to active or to deactivate the status
Private Sub CommandButton22_Click()
Dim bolInp_OK As Boolean

'Check if MCF is active or not
If CommandButton22.Caption = "Use MCF" Then
    'MCF cannot be activated if accelerated payment is active
    If [Accelerated_Payment_Flag] = 1 Then
        MsgBox "Accelerated Payment is active. If you want to use MCF, please deactivate Accelerated Payment."
        Exit Sub
    End If
    'Before activation a RORAC needs to be calculated to calculate the cash flow and determine payment dates
    bolInp_OK = fct_checkInput()
    If bolInp_OK = False Then
        Exit Sub
    End If
    'activate flag to write cash flow dates and structure at MCF sheet
    Worksheets("Index").Range("write_mcf").value = "yes"
    Call unprotectInput
    Call prcStartCalculation
    Worksheets("Manual_Cash_Flows").Range("Nom_CR_MCF").value = [Nom_CR]
    Worksheets("Index").Range("write_mcf").value = "no"
    Worksheets("Manual_Cash_Flows").Activate
    'unhide Button to change MCF
    CommandButton23.Visible = True
    CommandButton22.Caption = "Don't Use MCF"
    Application.StatusBar = "RORAC was successfully calculated"
    Call protectInput
Else
    'in case MCF is activate it will be deactivated
    Worksheets("Index").Range("Manual_CF_Flag") = 0
    CommandButton23.Visible = False
    CommandButton22.Caption = "Use MCF"
End If


End Sub

'Sub to open MCF-sheet to change MCF--> only visible if MCF is activated
Private Sub CommandButton23_Click()
If Not (Application.StatusBar = "RORAC was successfully calculated" Or Application.StatusBar = "Target RORAC was successfully calculated") Then
    MsgBox "Please calculate RORAC first"
    Exit Sub
End If
    Worksheets("Manual_Cash_Flows").Range("Nom_CR_MCF").value = [Nom_CR]
    Worksheets("Manual_Cash_Flows").Activate
End Sub

'sub to open form for adding add. collaterals--> only visible if one coll. is already added
Private Sub CommandButton7_Click()
Frm_Mul_Col.Show
End Sub


'Protect Sheet and defines the ScrollArea after activation
Private Sub Worksheet_Activate()
Call protectInput
Worksheets("New Input Mask").ScrollArea = "A1:Z100"
Worksheets("New Input Mask").Range("E8").Select

'Set command button postions
Worksheets("New Input Mask").CommandButton3.Left = 12
Worksheets("New Input Mask").CommandButton3.Height = 33
Worksheets("New Input Mask").CommandButton3.Top = 48
Worksheets("New Input Mask").CommandButton3.Width = 66

Worksheets("New Input Mask").CommandButton10.Left = 77.25
Worksheets("New Input Mask").CommandButton10.Height = 33
Worksheets("New Input Mask").CommandButton10.Top = 48
Worksheets("New Input Mask").CommandButton10.Width = 53.25

Worksheets("New Input Mask").CommandButton5.Left = 129.6
Worksheets("New Input Mask").CommandButton5.Height = 33
Worksheets("New Input Mask").CommandButton5.Top = 48
Worksheets("New Input Mask").CommandButton5.Width = 81

Worksheets("New Input Mask").CommandButton1.Left = 209.25
Worksheets("New Input Mask").CommandButton1.Height = 33
Worksheets("New Input Mask").CommandButton1.Top = 48
Worksheets("New Input Mask").CommandButton1.Width = 63

Worksheets("New Input Mask").CommandButton18.Left = 270.75
Worksheets("New Input Mask").CommandButton18.Height = 33
Worksheets("New Input Mask").CommandButton18.Top = 48
Worksheets("New Input Mask").CommandButton18.Width = 49.5

Worksheets("New Input Mask").CommandButton2.Left = 321
Worksheets("New Input Mask").CommandButton2.Height = 33
Worksheets("New Input Mask").CommandButton2.Top = 48
Worksheets("New Input Mask").CommandButton2.Width = 84.75

Worksheets("New Input Mask").CommandButton9.Left = 405.75
Worksheets("New Input Mask").CommandButton9.Height = 33
Worksheets("New Input Mask").CommandButton9.Top = 48
Worksheets("New Input Mask").CommandButton9.Width = 80.25

Worksheets("New Input Mask").CommandButton20.Left = 485.25
Worksheets("New Input Mask").CommandButton20.Height = 33
Worksheets("New Input Mask").CommandButton20.Top = 48
Worksheets("New Input Mask").CommandButton20.Width = 72

Worksheets("New Input Mask").CommandButton17.Left = 556.5
Worksheets("New Input Mask").CommandButton17.Height = 33
Worksheets("New Input Mask").CommandButton17.Top = 48
Worksheets("New Input Mask").CommandButton17.Width = 65.25

Worksheets("New Input Mask").CommandButton16.Left = 619.5
Worksheets("New Input Mask").CommandButton16.Height = 33
Worksheets("New Input Mask").CommandButton16.Top = 48
Worksheets("New Input Mask").CommandButton16.Width = 66

Worksheets("New Input Mask").CommandButton19.Left = 685
Worksheets("New Input Mask").CommandButton19.Height = 33
Worksheets("New Input Mask").CommandButton19.Top = 48
Worksheets("New Input Mask").CommandButton19.Width = 101

With ActiveSheet.PageSetup
'    .LeftFooter = "DFS RORAC Tool Version " & APPVERSION
End With
End Sub

'sub to reset application status bar in case of change from customer rate to dfs buy rate or vice versa
Private Sub ComboBox3_Change()
Application.StatusBar = ""
End Sub

'sub to open form for target pricing --> only feasible if RORAC for current inputs was calculated
Private Sub CommandButton10_Click()
Dim zelle As Range
Dim lItem As Integer
Dim intLastInst

lItem = 0

If Not (Application.StatusBar = "RORAC was successfully calculated" Or Application.StatusBar = "Target RORAC was successfully calculated") Then
    MsgBox "Please calculate RORAC first"
    Exit Sub
End If

Frm_Target_Calc.Show

End Sub

'sub to open form that contains LGD chart --> only visible if RORAC for current inputs was calculated
Private Sub CommandButton11_Click()
frm_LGDDev.Show
End Sub

'Sub to open cash flow analysis sheet--> in case of mexico additional columns will be unhidden
Private Sub CommandButton1_Click()

Worksheets("Cash Flow Analysis").Unprotect Password:="Blattschutz"
If [Country_Short] = "MEX" Then
    Worksheets("Cash Flow Analysis").Columns("L:M").EntireColumn.Hidden = False
Else
    Worksheets("Cash Flow Analysis").Columns("L:M").EntireColumn.Hidden = True
End If
Worksheets("Cash Flow Analysis").Activate
Worksheets("Cash Flow Analysis").Protect Password:="Blattschutz"

End Sub

'Copies Deal to Portfolio View -->only feasible if RORAC for current inputs was calculated
Private Sub CommandButton2_Click()
On Error GoTo Fehler

If Not (Application.StatusBar = "RORAC was successfully calculated" Or Application.StatusBar = "Target RORAC was successfully calculated") Then
    MsgBox "Please calculate RORAC first"
    Exit Sub
End If

Dim wksPortfolio As Worksheet
Dim wksIndex As Worksheet
Dim wksinput As Worksheet
Dim clm As Integer
Dim intSure As Integer

Set wksPortfolio = Sheets("Portfolio")
Set wksinput = Sheets("New Input Mask")
Set wksIndex = Sheets("Index")

clm = 10

'Check if already 10 deals are stored within portfolio view
If Worksheets("Portfolio").Range("BC10").value = 1 Then
    Do While wksPortfolio.Cells(11, clm).value <> ""
        clm = clm + 3
        If clm > 52 Then
            MsgBox ("Too many Deals in Portfolio, please delete one or consolidate with another deal!")
            Exit Sub
        End If
    Loop
Else
    Do While wksPortfolio.Cells(11, clm).value <> ""
    clm = clm + 3
    If clm > 37 Then
        MsgBox ("Too many Deals in Portfolio, please enable more deals by pressing ""Show 10+ Deals"" on the portfolio view!")
        Exit Sub
    End If
Loop

End If

'Check if deals shall be copied even without a customer name
If Worksheets("New Input Mask").Range("L8").value = "" Then
    intSure = MsgBox("No Customer Name is entered. Do you want to continue?", vbYesNo, "Missing Customer Name" & Str(dealnr))
    If intSure = 7 Then
        Exit Sub
    End If
End If

Call unprotectInput
Worksheets("Portfolio").Unprotect Password:="Blattschutz"

'Copy data to portfolio sheet
With wksPortfolio
     .Cells(5, clm).value = [Interest_as_of]
     .Cells(9, clm).value = wksinput.Range("L8").value
     If wksinput.Range("U19").value <> "" And wksinput.Range("L19").value <> "" Then
        .Cells(10, clm).value = wksinput.Range("E10").value & " / " & Application.Round(Worksheets("Index").Range("final_PD").value, 2) & "% / " & Application.Round(((Worksheets("Index").Range("Mdl_CoCR").value * 10000) / Worksheets("Index").Range("final_PD").value), 2) & "% / Y"
     Else
        .Cells(10, clm).value = wksinput.Range("E10").value & " / " & Application.Round(Worksheets("Index").Range("final_PD").value, 2) & "% / " & Application.Round(((Worksheets("Index").Range("Mdl_CoCR").value * 10000) / Worksheets("Index").Range("final_PD").value), 2) & "% / N"
     End If
     'Collateral information will be displayed as a comment
     If wksIndex.Range("i322").value <> "" Then
        .Cells(10, clm).AddComment
        .Cells(10, clm).Comment.text text:=wksIndex.Range("i322") & Chr(10) & wksIndex.Range("i323") & Chr(10) & wksIndex.Range("i324")
        .Cells(10, clm).Comment.Shape.TextFrame.AutoSize = True
     End If
     '***excluding JPN as most their products are new and used, added on 21.01.2016***
     If [Country_Short] <> "JPN" And InStr(1, UCase(wksinput.Range("E25").value), "USED") Then
        .Cells(11, clm).value = wksinput.Range("E19").value & " (Used)"
     Else
        .Cells(11, clm).value = wksinput.Range("E19").value & " (" & [New_Used] & ")"
     End If
     .Cells(108, clm).value = wksinput.Range("E19").value
     .Cells(12, clm).value = wksinput.Range("e25").value
     .Cells(13, clm).value = wksinput.Range("H45").value * wksinput.Range("L17").value * 0.001
     .Cells(13, clm + 1).value = "T" & Left(wksIndex.Range("Deal_Currency"), 3)
     .Cells(14, clm).value = wksinput.Range("H47").value
     .Cells(15, clm).value = wksinput.Range("L17").value
     .Cells(16, clm).value = (wksinput.Range("h29").value + wksinput.Range("H31").value) * wksinput.Range("L17").value * 0.001
     .Cells(16, clm + 1).value = "T" & Left(wksIndex.Range("Deal_Currency"), 3)
     .Cells(17, clm).value = wksinput.Range("H33").value * wksinput.Range("L17").value * 0.001
     .Cells(17, clm + 1).value = "T" & Left(wksIndex.Range("Deal_Currency"), 3)
     .Cells(18, clm).value = wksIndex.Range("mdl_Installment").value * [Number_of_Vehicles]
     .Cells(18, clm + 1).value = Left(wksIndex.Range("Deal_Currency"), 3)
     .Cells(19, clm).value = wksinput.Range("H35").value * wksinput.Range("L17").value * 0.001
     .Cells(19, clm + 1).value = "T" & wksIndex.Range("Deal_Currency")
     .Cells(22, clm).value = wksinput.Range("V25").value * 100
     .Cells(23, clm).value = wksinput.Range("V27").value * 100
     .Cells(24, clm).value = wksinput.Range("V29").value * 100
     .Cells(25, clm).value = wksinput.Range("V31").value * 100
     .Cells(26, clm).value = wksinput.Range("V33").value * 100
     .Cells(27, clm).value = wksinput.Range("V35").value * 100
     .Cells(28, clm).value = wksinput.Range("V37").value * 100
     .Cells(29, clm).value = wksinput.Range("V39").value * 100
     .Cells(30, clm).value = wksinput.Range("V41").value * 100
     .Cells(31, clm).value = wksinput.Range("V43").value * 100
     .Cells(32, clm).value = wksinput.Range("V45").value * 100
     .Cells(33, clm).value = wksinput.Range("V47").value * 100
     .Cells(35, clm).value = wksinput.Range("V49").value * 100
     .Cells(36, clm).value = wksIndex.Range("m154").value / 1000
     .Cells(36, clm + 1).value = "T" & Left(wksIndex.Range("Deal_Currency"), 3)
     .Cells(38, clm).value = wksIndex.Range("m151").value * 100
     .Cells(39, clm).value = wksinput.Range("E5").value
     .Cells(40, clm).value = wksinput.Range("L5").value
     
     'In case of US R/S value is copied from local sheet
     If [Country_Short] = "USA" Then
        .Cells(41, clm).value = [R_S_US_Value]
     Else
        .Cells(41, clm).value = wksinput.Range("E10").value
     End If
     
     'Flag if at least one collateral is entered
     If wksinput.Range("U19").value <> "" And wksinput.Range("L19").value <> "" Then
        .Cells(10, clm).value = .Cells(41, clm).value & " / " & Application.Round(Worksheets("Index").Range("final_PD").value, 2) & "% / " & Application.Round(((Worksheets("Index").Range("Mdl_CoCR").value * 10000) / Worksheets("Index").Range("final_PD").value), 2) & "% / Y"
     Else
        .Cells(10, clm).value = .Cells(41, clm).value & " / " & Application.Round(Worksheets("Index").Range("final_PD").value, 2) & "% / " & Application.Round(((Worksheets("Index").Range("Mdl_CoCR").value * 10000) / Worksheets("Index").Range("final_PD").value), 2) & "% / N"
     End If
     .Cells(42, clm).value = wksinput.Range("h60").value
     .Cells(43, clm).value = wksinput.Range("h62").value
     .Cells(44, clm).value = wksinput.Range("U19").value
     .Cells(45, clm).value = wksinput.Range("h64").value
     .Cells(46, clm).value = ComboBox5.value
     .Cells(47, clm).value = wksinput.Range("E12").value
     .Cells(48, clm).value = wksinput.Range("E17").value
     .Cells(49, clm).value = wksinput.Range("U17").value
     .Cells(50, clm).value = wksinput.Range("L19").value
     .Cells(51, clm).value = wksinput.Range("Q19").value
     .Cells(52, clm).value = Worksheets("Index").Range("Manual_CF_Flag")
     .Cells(53, clm).value = wksinput.Range("e25").value
'     .Cells(54, clm).value = wksinput.Range("D27").value
     .Cells(54, clm).value = wksIndex.Range("Deal_Currency")
     .Cells(55, clm).value = wksinput.Range("G27").value
     .Cells(56, clm).value = wksinput.Range("h29").value
     .Cells(57, clm).value = wksinput.Range("H31").value
     .Cells(58, clm).value = wksinput.Range("E33").value
     .Cells(59, clm).value = ComboBox1.value
     .Cells(60, clm).value = wksinput.Range("E37").value
     .Cells(61, clm).value = wksinput.Range("H39").value
     .Cells(62, clm).value = wksinput.Range("H41").value
     .Cells(63, clm).value = wksinput.Range("H43").value
     .Cells(64, clm).value = ComboBox4.value
     .Cells(65, clm).value = wksinput.Range("E45").value
     .Cells(66, clm).value = ComboBox2.value
     .Cells(67, clm).value = wksinput.Range("E49").value
     .Cells(68, clm).value = wksinput.Range("H49").value
     .Cells(69, clm).value = wksinput.Range("E52").value
     .Cells(70, clm).value = wksinput.Range("H52").value
     .Cells(71, clm).value = wksinput.Range("E54").value
     .Cells(72, clm).value = ComboBox3.value
     .Cells(73, clm).value = wksinput.Range("H54").value
     .Cells(74, clm).value = wksinput.Range("E58").value
     .Cells(75, clm).value = wksinput.Range("H58").value
     .Cells(76, clm).value = wksinput.Range("E60").value
     .Cells(78, clm).value = wksinput.Range("E64").value
     .Cells(79, clm).value = wksinput.Range("E66").value
     .Cells(80, clm).value = wksinput.Range("E68").value
     .Cells(81, clm).value = wksinput.Range("E70").value
     .Cells(82, clm).value = [Interest_as_of]
     'For US additional information from the local sheet will be copied due to the fact that they need them in the repository
     If [Country_Short] = "USA" Then
        .Cells(83, clm).value = [Approval_US]
        .Cells(84, clm).value = [BP_Factor]
        .Cells(85, clm).value = [FAS13_US]
        .Cells(86, clm).value = [R_S_US]
        .Cells(87, clm).value = [US_Lease]
        .Cells(88, clm).value = [US_Lease_P]
     End If
     'For France additional information from the local sheet will be copied due to the fact that they need them in the repository
     If [Country_Short] = "FRA" Then
        .Cells(143, clm) = [Differe]
        .Cells(144, clm) = [Day_Differe]
        .Cells(145, clm) = [Assurance]
        .Cells(146, clm) = [Assurance1]
        .Cells(147, clm) = [Assurance_Typ1]
        .Cells(148, clm) = [Produits_financiers1]
        .Cells(149, clm) = [Assurance2]
        .Cells(150, clm) = [Assurance_Typ2]
        .Cells(151, clm) = [Produits_financiers2]
        .Cells(153, clm) = [Per_Annee]
        .Cells(154, clm) = [Duree_Contrat]
        .Cells(155, clm) = [Montant_annuel]
        .Cells(156, clm) = [perc_dur_contrat]
        .Cells(157, clm) = [Commission_Fixed]
     End If
     
     .Cells(93, clm).value = wksIndex.Range("M163").value
     .Cells(94, clm).value = wksIndex.Range("M130").value
     .Cells(95, clm).value = wksIndex.Range("M131").value
     .Cells(96, clm).value = wksIndex.Range("M132").value
     .Cells(97, clm).value = wksIndex.Range("M133").value
     .Cells(98, clm).value = wksIndex.Range("M134").value
     .Cells(99, clm).value = wksIndex.Range("M135").value
     .Cells(100, clm).value = wksIndex.Range("M136").value
     .Cells(101, clm).value = wksIndex.Range("M137").value
     .Cells(102, clm).value = wksIndex.Range("M138").value
     .Cells(103, clm).value = wksIndex.Range("M139").value
     .Cells(104, clm).value = wksIndex.Range("M140").value
     .Cells(105, clm).value = wksIndex.Range("M141").value
     .Cells(106, clm).value = wksIndex.Range("M142").value
     .Cells(107, clm).value = wksIndex.Range("M143").value
     .Cells(109, clm).value = wksinput.Range("Q12").value
     .Cells(110, clm).value = wksIndex.Range("M144").value
     .Cells(111, clm).value = wksIndex.Range("M145").value
     .Cells(112, clm).value = wksIndex.Range("M146").value
     .Cells(38, 3).value = wksIndex.Range("M148").value
     .Cells(113, clm).value = wksIndex.Range("M148").value
     .Cells(114, clm).value = wksIndex.Range("M153").value
     .Cells(115, clm).value = wksinput.Range("L58").value
     .Cells(115, clm).value = wksinput.Range("L74").value
     .Cells(116, clm).value = ComboBox6.value
     .Cells(117, clm).value = Worksheets("Index").[mdl_PV_Outstanding] * [Number_of_Vehicles]
     .Cells(118, clm).value = [PD_Interim]
     .Cells(119, clm).value = wksIndex.Range("C323").value
     .Cells(120, clm).value = wksIndex.Range("D323").value
     .Cells(121, clm).value = wksIndex.Range("E323").value
     .Cells(122, clm).value = wksIndex.Range("C324").value
     .Cells(123, clm).value = wksIndex.Range("D324").value
     .Cells(124, clm).value = wksIndex.Range("E324").value
     .Cells(125, clm).value = wksinput.Range("E8").value
     .Cells(126, clm).value = wksIndex.Range("FX_Rate").value
End With

' MEXICO ONLY - Added by Carsten Sturmann
'The following code applies to the Mexico version of the RORAC tool only. Mexico has a very detailed
'customer output sheet which contains cash flow details and contract information. When hitting the
'"copy to portfolio" button, this information is summarized and copied into the local output sheet.

If Worksheets("New Input Mask").Range("E5").value = "MEX" Then
    Dim X As Integer
    Dim n As Integer
    
    
    Worksheets("Local_output").Unprotect Password:="Blattschutz"
     
    'copy model name, finance product to output sheet
    Worksheets("Local_output").Cells(20, clm - 5) = Worksheets("Local_Sheet").Range("H5").value
    Worksheets("Local_output").Cells(21, clm - 5) = Worksheets("New Input Mask").Range("E25").value
     
    'copy unit price (incl tax) into output sheet. For operating leases that information is left blank
    If Worksheets("New Input Mask").Range("E25").value = "Operating Lease" Then
        Worksheets("Local_output").Cells(22, clm - 5) = ""
    Else
        Worksheets("Local_output").Cells(22, clm - 5) = Worksheets("Local_Sheet").Range("M9").value
    End If
     
    'copy currency and vehicle insurance (incl tax) to output sheet
    Worksheets("Local_output").Cells(23, clm - 5) = Worksheets("New Input Mask").Range("D27").value
    Worksheets("Local_output").Cells(24, clm - 5) = Worksheets("Local_Sheet").Range("M11").value
    
    'copy base amount for finance contract. For Operating lease that value is left blank.
    'for finance lease = selling price excl additional finance items + seguro unidad s/IVA
    'for standard finance contract = selling price incl tax + seguro unidad c/IVA
    If Worksheets("New Input Mask").Range("E25").value = "Operating Lease" Then
        Worksheets("Local_output").Cells(25, clm - 5) = ""
    Else
        If Worksheets("New Input Mask").Range("E25").value = "Finance Lease" Then
            Worksheets("Local_output").Cells(25, clm - 5) = Worksheets("New Input Mask").Range("H29").value + Worksheets("Local_Sheet").Range("K11").value
        Else
            Worksheets("Local_output").Cells(25, clm - 5) = Worksheets("Local_Sheet").Range("M11").value + Worksheets("Local_Sheet").Range("M9").value
        End If
    End If
     
    'copy down-payment to output sheet
    'For finance lease and operating lease, the value shown to the customer is equal to
    'down-payment amount x (1 + tax rate) and referred to as upfront rent payment
    If Worksheets("New Input Mask").Range("E25").value = "Standard Financing" Then
        Worksheets("Local_output").Cells(27, clm - 5) = Worksheets("New Input Mask").Range("H33").value
    Else
        Worksheets("Local_output").Cells(27, clm - 5) = Worksheets("New Input Mask").Range("H33").value * (1 + Worksheets("Local_Sheet").Range("M5").value)
    End If
     
    'copy deposits and commission payments due from customer into output sheet
    Worksheets("Local_output").Cells(28, clm - 5) = Worksheets("New Input Mask").Range("H43").value
    Worksheets("Local_output").Cells(29, clm - 5) = Worksheets("Local_Sheet").Range("M25").value
     
    'copy and translate payment mode into output sheet
    If Worksheets("New Input Mask").Range("H52").value = "In Arrears" Then
        Worksheets("Local_output").Cells(30, clm - 5) = "Vencidas"
    Else
        Worksheets("Local_output").Cells(30, clm - 5) = "Anticipadas"
    End If
     
    'copy finance amount into output sheet. Left blank for operating leases
    If Worksheets("New Input Mask").Range("E25").value = "Operating Lease" Then
        Worksheets("Local_output").Cells(31, clm - 5) = ""
    Else
        Worksheets("Local_output").Cells(31, clm - 5) = Worksheets("New Input Mask").Range("H35").value
    End If
     
    'copy contract term
    Worksheets("Local_output").Cells(32, clm - 5) = Worksheets("New Input Mask").Range("H47").value
     
    'copy interest rate into output sheet. Left blank for operating leases
    If Worksheets("New Input Mask").Range("E25").value = "Operating Lease" Then
        Worksheets("Local_output").Cells(33, clm - 5) = ""
    Else
        Worksheets("Local_output").Cells(33, clm - 5) = Worksheets("New Input Mask").Range("V25").value
    End If
     
    'copy initial payment amount to output sheet.
    'For payments in arrears, it consists of down-payment / upfront rent payment, deposits and commission due by customer
    'If payment mode is in advance and product is finance lease , then the first instalment (x 1 + tax rate) at contract signing is included here.
    'If payment mode is in advance and product is not finance lease, then the payment as shown on cash flow sheet incl tax (column N) is added
    'The Tax on the monthly instalment as shown on the cash flow tab is allocated either based on monthly payment, interest or principal. Hence the two different fields are used.
    If Worksheets("New Input Mask").Range("H52").value = "In Arrears" Then
        Worksheets("Local_output").Cells(34, clm - 5) = Worksheets("Local_Output").Cells(27, clm - 5).value + Worksheets("Local_Output").Cells(28, clm - 5).value + Worksheets("Local_Output").Cells(29, clm - 5).value
    Else
        If Worksheets("New Input Mask").Range("E25").value = "Finance Lease" Then
            Worksheets("Local_output").Cells(34, clm - 5) = Worksheets("Local_Output").Cells(27, clm - 5).value + Worksheets("Local_Output").Cells(28, clm - 5).value + Worksheets("Local_Output").Cells(29, clm - 5).value + (Worksheets("Cash Flow Analysis").Range("B4").value * (1 + Worksheets("Local_Sheet").Range("M5").value))
        Else
            Worksheets("Local_output").Cells(34, clm - 5) = Worksheets("Local_Output").Cells(27, clm - 5).value + Worksheets("Local_Output").Cells(28, clm - 5).value + Worksheets("Local_Output").Cells(29, clm - 5).value + Worksheets("Cash Flow Analysis").Range("N4").value
        End If
    End If
     
    'Final payment amount. Left blank on output sheet for operating lease and finance lease. Otherwise equal to RV/balloon
    If Worksheets("New Input Mask").Range("E25").value = "Operating Lease" Or Worksheets("New Input Mask").Range("E25").value = "Finance Lease" Then
        Worksheets("Local_output").Cells(35, clm - 5) = ""
    Else
        Worksheets("Local_output").Cells(35, clm - 5) = Worksheets("New Input Mask").Range("H45").value
    End If
     
    'Copy number of units
    Worksheets("Local_output").Cells(36, clm - 5) = Worksheets("New Input Mask").Range("L17").value
     
    'Copy opcion de compra (finance leases only). This is an additional end of term payment due by customer
    If Worksheets("New Input Mask").Range("E25").value = "Finance Lease" Then
        Worksheets("Local_output").Cells(37, clm - 5) = Worksheets("Local_Sheet").Range("M29").value
    Else
    End If
     
    'Copy monthly payment to output sheet.
    If Worksheets("New Input Mask").Range("E25").value = "Operating Lease" Then
        Worksheets("Local_output").Cells(39, clm - 5) = Worksheets("Cash Flow Analysis").Range("N4").value
    Else
        Worksheets("Local_output").Cells(39, clm - 5) = Worksheets("Index").Range("Customer_Instalment").value
    End If
     
     
    'In the following section the "CAT" is calculated. This is a legal requirement for Mexico and represents the
    'customer's deal IRR based on all the customer payments, deposits, etc that are due throughout the contract.
    'As it includes items such as security deposits, it is not equal to the DFS buy rate and needs to be determined
    'separatly by copying the cash flow items into the output sheet (hidden rows starting with row 90) and derive
    'the IRR there.
    'Furthermore, the customer amort schedule is populated within the same loop. That cash flow schedule is visible
    'for each deal in the printable section of the local output sheet
     
    'For CAT, start with: - (amount financed) + security deposit + commission + value for life insurance (Cell U11 on local sheet)
    Worksheets("Local_output").Cells(90, clm - 5) = -Worksheets("Cash Flow Analysis").Range("F3").value + Worksheets("New Input Mask").Range("H43").value + Worksheets("Local_sheet").Range("K25").value + Worksheets("Local_sheet").Range("U11").value
     
    Worksheets("Cash Flow Analysis").Unprotect Password:="Blattschutz"
    
    'After that copy cash flow consisting of monthly payments. Loop through as many rows as payment months + 1
    n = Worksheets("New Input Mask").Range("H47").value + 1
    'If Worksheets("New Input Mask").Range("H45").value > 0 Then n = n + 1
      
    'If security deposit is returned one month after final instalment, look for that additional cash flow month
    If Worksheets("Local_Sheet").Range("Y18").value = 3 Then n = n + 1
    For X = 1 To n
    
        'This row starts populating the CAT calc cash flows for principal + Interest + Deposit cash flows starting with data in row 4 from cash flow analysis tab
        Worksheets("Local_output").Cells(90 + X, clm - 5) = Worksheets("Cash Flow Analysis").Cells(3 + X, 8).value + Worksheets("Cash Flow Analysis").Cells(3 + X, 10).value + Worksheets("Cash Flow Analysis").Cells(3 + X, 18).value
    
        'The remaining code below populates the customer relevant cash flow schedule as shown on the print out from local output tab
        Worksheets("Local_output").Cells(2 + X, 19 + ((clm - 7) / 3) * 17) = X - 1
        Worksheets("Local_output").Cells(2 + X, 20 + ((clm - 7) / 3) * 17) = Worksheets("Cash Flow Analysis").Cells(2 + X, 1).value
        Worksheets("Local_output").Cells(2 + X, 21 + ((clm - 7) / 3) * 17) = Worksheets("Cash Flow Analysis").Cells(2 + X, 6).value
        Worksheets("Local_output").Cells(2 + X, 22 + ((clm - 7) / 3) * 17) = Worksheets("Cash Flow Analysis").Cells(2 + X, 8).value
        Worksheets("Local_output").Cells(2 + X, 23 + ((clm - 7) / 3) * 17) = Worksheets("Cash Flow Analysis").Cells(2 + X, 10).value
        Worksheets("Local_output").Cells(2 + X, 24 + ((clm - 7) / 3) * 17) = Worksheets("Cash Flow Analysis").Cells(2 + X, 12).value
        Worksheets("Local_output").Cells(2 + X, 25 + ((clm - 7) / 3) * 17) = Worksheets("Cash Flow Analysis").Cells(2 + X, 14).value
        Worksheets("Local_output").Cells(2 + X, 27 + ((clm - 7) / 3) * 17) = Worksheets("Cash Flow Analysis").Cells(2 + X, 18).value
    Next

    'Opcion the Compra is an end of term payment for finance leases, but not a balloon. Does not impact monthly payment
    Worksheets("Local_output").Cells(2 + X, 19 + ((clm - 7) / 3) * 17) = "Opción"
    Worksheets("Local_output").Cells(2 + X, 23 + ((clm - 7) / 3) * 17) = Worksheets("Local_sheet").Range("K29").value


    Worksheets("Cash Flow Analysis").Protect Password:="Blattschutz"
    Worksheets("Local_output").Protect Password:="Blattschutz"

End If

' MEXICO ONLY - End
Application.CalculateFullRebuild
Call protectInput
Worksheets("Portfolio").Protect Password:="Blattschutz"
Worksheets("New Input Mask").Activate

Exit Sub
Fehler: MsgBox ("Not possible to copy Deal. Please calculate RORAC First")
Call protectInput
Worksheets("Portfolio").Protect Password:="Blattschutz"
Worksheets("New Input Mask").Activate

End Sub

'Sub to calculate RORAC
Private Sub CommandButton3_Click()

Call unprotectInput

Dim zelle As Range
Dim lItem As Integer
Dim AnzMonths As Integer
Dim intLastInst
Dim bolInp_OK As Boolean

lItem = 0

If [Company] = "(FRA) Mercedes-Benz Financial Services France S.A. (Dealer)" Then
Call France_Dealer
End If

'Check if all necessary inputs are filled and valid--> if not calculation will be aborted
bolInp_OK = fct_checkInput()

If bolInp_OK = False Then
    Call protectInput
    Exit Sub
End If

'MessageBox If stored Interest and Spreads older than one week or one month (Exception for Mexico, Spain and Thailand)
If Worksheets("Index").Range("Expiry_Date_IS") < Date And [Country_Short] <> "MEX" Then
    If [Country_Short] = "RUS" Then
        MsgBox ("Stored Interests and Spreads are older than one week (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    ElseIf [Country_Short] = "THA" Then
        MsgBox ("Stored Interests and Spreads are older than two weeks (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    Else
        MsgBox ("Stored Interests and Spreads are older than one month (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    End If
End If

'MessageBox If stored Manual Cash Flow is used
If Worksheets("Index").Range("Manual_CF_Flag") = 1 Then
    MsgBox ("Manual Cash Flow is active and will be considered for calculation!")
End If

'MessageBox If more than one collateral is added
If Worksheets("Index").Range("C323").value <> "" Or Worksheets("Index").Range("C324").value <> "" Then
    MsgBox ("More than one additional collateral was added and will be considered for calculation!")
End If

'MessageBox If stored Acceleraded Payment is used
If Worksheets("Index").Range("Accelerated_Payment_Flag") = 1 Then
    MsgBox ("Accelerated Payment is active and will be considered for calculation!")
    Worksheets("Index").Range("write_mcf").value = "yes"
    Worksheets("Index").Range("Accelerated_Payment_Flag") = 0
    Call prcStartCalculation
    Worksheets("Index").Range("write_mcf").value = "no"
    Worksheets("Index").Range("Accelerated_Payment_Flag") = 1
    Application.Calculate
End If


'Start RORAC-Calculation
Call prcStartCalculation

Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "RORAC was successfully calculated"

'Exception for France Fleet Mgm; there is a check whether the values for upfront and periodic payments that are calculated on the local sheet match to the entries on the
'Input Mask; if there is a bigger deviations (limit is currently set at +- 0.1) the user is asked whether he wants to take over the new values from the local sheet or not
'The deviation is calculated in the [check_local_sheet_france] cell on the local sheet

If [Company] = "(FRA) Mercedes-Benz Financial Services France S.A. (Fleet)" Then
If ([check_local_sheet_france] > 0.1 Or [check_local_sheet_france] < -0.1) Then
    Dim entryvalue As Byte
    entryvalue = MsgBox(prompt:="Les valeurs saisies dans RoRAC ont changé et sont différentes de celles utilisées dans la Local Sheet. Souhaitez-vous les mettre à jour ?", Buttons:=vbYesNo)
        If entryvalue = vbYes Then
        Call France
        Else
        Exit Sub
        End If
End If
End If
End Sub

'Shows Main Form of Parameter Update
Private Sub CommandButton4_Click()
Frm_Password.Show
End Sub

'Show or hide the additional Information--> Alignment of buttons depends on the existence of manual PD row
Private Sub CommandButton5_Click()

Call unprotectInput

'check if add. information shall be hidden or unhidden
If Worksheets("New Input Mask").Rows("57:72").Hidden = True Then
    Worksheets("New Input Mask").Rows("57:72").Hidden = False
    Worksheets("New Input Mask").Rows("73:87").Hidden = True
    'CommandButton6.Visible = True
    'CommandButton13.Visible = True
    CommandButton22.Visible = True
    CommandButton4.Visible = True
    Label1.Visible = True
    If CommandButton22.Caption = "Use MCF" Then
        CommandButton23.Visible = False
    Else
        CommandButton23.Visible = True
    End If
    
    'Position List Box for Button for Calendar depending on the Manual PD
    If Worksheets("New Input Mask").Rows("12:13").Hidden = False Then
        'CommandButton13.Top = 717
        'CommandButton13.Left = 179.25
        CommandButton22.Top = 720.75
        CommandButton22.Left = 238.5
        CommandButton23.Top = 741.75
        CommandButton23.Left = 238.5
        If [Country_Short] <> "MEX" Then
            CommandButton4.Top = 580.5
            CommandButton4.Left = 18
        End If
        Label1.Top = 705.75
        Label1.Left = 235.5
        If Worksheets("New Input Mask").Range("E60").value = "Yes" Then
            ComboBox7.Visible = True
            ComboBox7.Left = 75
            ComboBox7.Top = 647.25
        Else
            ComboBox7.Visible = False
        End If
    Else
        'CommandButton13.Top = 699.75
        'CommandButton13.Left = 179.25
        CommandButton22.Top = 703.5
        CommandButton22.Left = 238.5
        CommandButton23.Top = 724.5
        CommandButton23.Left = 238.5
        Label1.Top = 688.5
        Label1.Left = 235.5
        If [Country_Short] <> "MEX" Then
            CommandButton4.Top = 563.25
            CommandButton4.Left = 18
        End If
        If Worksheets("New Input Mask").Range("E60").value = "Yes" Then
            ComboBox7.Visible = True
            ComboBox7.Left = 75
            ComboBox7.Top = 630
        Else
            ComboBox7.Visible = False
        End If
    End If
    CommandButton5.Caption = "Hide Optional Information"
Else
    Worksheets("New Input Mask").Rows("57:72").Hidden = True
    Worksheets("New Input Mask").Rows("73:87").Hidden = False
    'CommandButton6.Visible = False
    'CommandButton13.Visible = False
    CommandButton22.Visible = False
    CommandButton23.Visible = False
    If [Country_Short] <> "MEX" Then
        CommandButton4.Visible = False
    End If
    ComboBox7.Visible = False
    Label1.Visible = False
    CommandButton5.Caption = "Show Optional Information"
    Worksheets("New Input Mask").Range("$H$29").Select
End If

Call protectInput

End Sub

'sub to open the calender to enter a first instalment date
Private Sub CommandButton6_Click()
Frm_First_Ins_Date.Show
End Sub

'sub to open the calender to enter a payout date
Private Sub CommandButton8_Click()
Frm_Payout_Date.Show
End Sub

'sub to open portfolio sheet
Private Sub CommandButton9_Click()
Worksheets("Portfolio").Activate
Application.EnableEvents = True
End Sub


'sub that starts in case of an change event
Private Sub Worksheet_Change(ByVal Target As Range)

'in case of an change on the input mask the current calcultation becomes invalid and status will be reseted and LGD button will be unhidden
Application.StatusBar = ""
Worksheets("New Input Mask").CommandButton11.Visible = False
Worksheets("Index").Range("M148") = ""
Worksheets("Index").Range("M155") = ""

'Creates Entity List for the selected Country
If Target.Address = "$E$5" Then
     On Error Resume Next
     Dim j As Integer
     j = 0
     Worksheets("INDEX").Range("Company_List_For_Sel_Country").value = ""
     Worksheets("New Input Mask").Cells(5, 12) = ""
     For i = 2 To 82
         If Worksheets("INDEX").Cells(i, 6) = Worksheets("New Input Mask").Cells(5, 5) Or Worksheets("INDEX").Cells(i, 3) = Worksheets("New Input Mask").Cells(5, 5) Then
             Worksheets("INDEX").Cells(85 + j, 1) = Worksheets("INDEX").Cells(i, 2)
             j = j + 1
         End If
     Next i
     Worksheets("New Input Mask").Cells(5, 12) = "Please choose an Entity"
End If
'Delete rating for ESP/FRA/UK when switching from fleet to dealer and back
If Target.Address = "$E$8" Then
     If ([Country_Short] = "FRA" Or [Country_Short] = "ESP" Or [Country_Short] = "GBR") Then
        Worksheets("New Input Mask").Range("E10").value = ""
     End If
End If
     
'after entity selection data will be loaded from data entities to index sheet
If Target.Address = "$L$5" Then
     'On Error Resume Next
     If Worksheets("New Input Mask").Range("L5").value = "" Or Worksheets("New Input Mask").Range("L5").value = "Please choose an Entity" Then
         Exit Sub
     End If
     
     '****lock guaranteed/non-guaranteed RV selection if normal UK FS business is chosen, added on Apr 22, 2016****
     If [Country_Short] = "GBR" Then
        If Worksheets("New Input Mask").Range("L5").value = "(GBR) Mercedes-Benz Financial Services UK Ltd" Then
            Worksheets("New Input Mask").Range("C45").value = "Balloon or Guaranteed RV"
            ComboBox4.Visible = False
        Else
            ComboBox4.Visible = True
        End If
     End If
     
     Call dataload(1)
      'Delete the setting from IDC and Downpayment to avoid confusion
     ComboBox1.value = ""
     ComboBox6.value = ""
     Worksheets("New Input Mask").Range("E8").value = Worksheets("Index").Range("d86").value
     'Delete Rating if we switch from one entity to other
     Worksheets("New Input Mask").Range("E10") = ""
     'Update the Product selection to trigger corresponding events
     Worksheets("New Input Mask").Range("E25").value = Worksheets("New Input Mask").Range("E25").value
     Exit Sub
End If


'check if entered Rating is a numeric value, exception for france due to the point that FRA enters text
If Target.Address = "$E$10" Then
    'Numeric and textual allowed
    If ([Country_Short] = "FRA" Or [Country_Short] = "ESP" Or [Country_Short] = "GBR") And Worksheets("New Input Mask").Range("E8") = "Dealer" Then
        If Application.WorksheetFunction.IsText(Worksheets("New Input Mask").Range("E10")) Then
            Select Case Worksheets("New Input Mask").Range("E10").value
                Case "", "AAA", "AA", "A", "BBB", "BB", "B", "CCC", "CC", "C", "D", "E", "F"
                Case Else12
                    MsgBox "Please enter a valid rating." & vbCrLf & "(AAA;AA;A;BBB;BB;BB;CCC;CC;C;D;E;F)"
                    Worksheets("New Input Mask").Range("E10").value = ""
                    Exit Sub
                End Select
        End If
        
          If IsNumeric(Worksheets("New Input Mask").Range("E10")) Then
            If (Worksheets("New Input Mask").Range("E10").value < 1 Or Worksheets("New Input Mask").Range("E10").value > 11) And Worksheets("New Input Mask").Range("E10").value <> "" Then
                MsgBox "Please enter a number between 1 and 11"
                Worksheets("New Input Mask").Range("E10").value = ""
                Exit Sub
            End If
            If Worksheets("New Input Mask").Range("E10").value > 11 Then
            If Worksheets("Index").Range("Borrower_Type").value = "Corporate Dealer" And Worksheets("Index").Range("Is_Bank_Branch_deal").value = 0 Then
                If Worksheets("Index").Range("N96").value = "" Then
                    MsgBox "No parameter for Rating 12 available, maximum is 11!"
                    Worksheets("New Input Mask").Range("E10").value = ""
                    Exit Sub
                End If
            End If
            End If
          End If
    Else
    If ([Country_Short] = "FRA" And Worksheets("New Input Mask").Range("E8") = "Fleet") Then
        Select Case Worksheets("New Input Mask").Range("E10").value
        Case "", "3++", "3+", "3", "4+", "4", "0", "5+", "5", "6", "7", "8", "9"
        Case Else
            MsgBox "Please enter a valid rating." & vbCrLf & "(3++;3+;3;4+;4;0;5+;5;6;7;8;9)"
            Worksheets("New Input Mask").Range("E10").value = ""
        End Select
     Else
    'Only numeric allowed
       If Not IsNumeric(Worksheets("New Input Mask").Range("E10").value) And Worksheets("New Input Mask").Range("E10").value <> "" Then
            MsgBox "Please enter a number between 1 and 11"
            Worksheets("New Input Mask").Range("E10").value = ""
            Exit Sub
        ElseIf (Worksheets("New Input Mask").Range("E10").value < 1 Or Worksheets("New Input Mask").Range("E10").value > 11) And Worksheets("New Input Mask").Range("E10").value <> "" Then
            MsgBox "Please enter a number between 1 and 11"
            Worksheets("New Input Mask").Range("E10").value = ""
            Exit Sub
        ElseIf Worksheets("New Input Mask").Range("E10").value > 10 Then
            
            If Worksheets("Index").Range("Borrower_Type").value = "Corporate Fleet" Then
                If Worksheets("Index").Range("N96").value = "" Then
                    MsgBox "No parameter for Rating 11 available, maximum is 10!"
                    Worksheets("New Input Mask").Range("E10").value = ""
                    Exit Sub
                End If
            End If
            
            If Worksheets("Index").Range("Borrower_Type").value = "Corporate Dealer" And Not Worksheets("Index").Range("Is_Bank_Branch_deal") Then
                If Worksheets("Index").Range("N96").value = "" Then
                    MsgBox "No parameter for Rating 11 available, maximum is 10!"
                    Worksheets("New Input Mask").Range("E10").value = ""
                    Exit Sub
                End If
            End If
            
            If Worksheets("Index").Range("Borrower_Type").value = "Retail Private" Then
                If Worksheets("Index").Range("O96").value = "" Then
                    MsgBox "No parameter for ScoringClass 11 available, maximum is 10!"
                    Worksheets("New Input Mask").Range("E10").value = ""
                    Exit Sub
                End If
            End If
            
            If Worksheets("Index").Range("Borrower_Type").value = "Retail Small Business" Then
                If Worksheets("Index").Range("P96").value = "" Then
                    MsgBox "No parameter for ScoringClass 11 available, maximum is 10!"
                    Worksheets("New Input Mask").Range("E10").value = ""
                    Exit Sub
                End If
            End If
            
        End If
    End If
    
       
    End If
End If

'In case that one collateral is added the button to add more will be unhidden
If Target.Address = "$L$19" Or Target.Address = "$L$19:$O$19" Or Target.Address = "$U$19:$V$19" Or Target.Address = "$U$19" Then
    If Worksheets("New Input Mask").Range("L19").value = "" Or Worksheets("New Input Mask").Range("U19").value = "" Then
       CommandButton7.Visible = False
       Worksheets("Index").Range("C323:C324").value = ""
    Else
       CommandButton7.Visible = True
    End If
End If

'Exception for USA if manuel PD <> PD of Local Sheet
If Target.Address = "$E$8" And [Country_Short] = "USA" Then
    Worksheets("New Input Mask").Range("E12").value = ""
End If

'swith rating and scoring class depending on the borrowertype
If Target.Address = "$E$8" And [Country_Short] <> "USA" Then
Application.EnableEvents = False
    If Worksheets("Index").Range("Borrower_Type").value = "Retail Private" _
    Or Worksheets("Index").Range("Borrower_Type").value = "Retail Small Business" Then
        Call unprotectInput
        Worksheets("New Input Mask").Range("C10").value = "DFS Scoring Class"
        Call protectInput
    Else
        Call unprotectInput
        Worksheets("New Input Mask").Range("C10").value = "DFS Rating"
        Call protectInput
    End If
Application.EnableEvents = True
End If


'If target.Address = "$L$19" Or target.Address = "$L$19:$O$19" Or target.Address = "$U$19:$V$19" Or target.Address = "$U$19" Then
'    If Worksheets("New Input Mask").Range("L19").value = "" Or Worksheets("New Input Mask").Range("U19").value = "" Then
'       CommandButton7.Visible = False
'       Worksheets("Index").Range("C323:C324").value = ""
'    Else
'       CommandButton7.Visible = True
'    End If
'End If

'if clause to handle accelerated payments
If Target.Address = "$E$60" Then
    'On Error Resume Next
    Call unprotectInput
    If Worksheets("New Input Mask").Range("E60").value = "Yes" Then
        'Accelerated payment can only be used if MCF is deactivated
        If [Manual_CF_Flag] = 1 Then
            MsgBox "Manual Cash Flow is active. If you want to use the Accelerated Payment function, please deactivate Manual Cash Flow."
            Application.EnableEvents = False
            Worksheets("New Input Mask").Range("E60").value = "No"
            Application.EnableEvents = True
            Call protectInput
            Exit Sub
        End If
        'Cells and combobox to enter accelerated payment will be unlocked and pre settings will be added
        Worksheets("New Input Mask").Range("E61").Locked = False
        Worksheets("New Input Mask").Range("E62").Locked = False
        ComboBox7.Visible = True
        ComboBox7.value = [Deal_Currency]
        Worksheets("New Input Mask").Range("E61").value = [Cont_RV] + 1000
        Application.Calculate
        'calculation of RORAC with current settings needs to be successfull otherwise activation will be aborted
        Worksheets("Index").Range("write_mcf").value = "yes"
        bolInp_OK = fct_checkInput()
        If bolInp_OK = False Then
            Worksheets("New Input Mask").Range("E61").Locked = True
            Worksheets("New Input Mask").Range("E62").Locked = True
            ComboBox7.Visible = False
            Application.EnableEvents = False
            Worksheets("New Input Mask").Range("E60").value = "No"
            Application.EnableEvents = True
            Call protectInput
            Exit Sub
        End If
        
        Call prcStartCalculation
        Worksheets("Index").Range("write_mcf").value = "no"
        Worksheets("New Input Mask").Range("E62").value = Worksheets("Manual_Cash_Flows").Range("B3").value
        [Accelerated_Payment_Flag] = 1
        'Call LGD_button_CalcDate
        Call protectInput
        Exit Sub
    Else
        'deactivation of accelerated payment
        [Accelerated_Payment_Flag] = 0
        Worksheets("New Input Mask").Range("E61").Locked = True
        Worksheets("New Input Mask").Range("E62").Locked = True
        ComboBox7.Visible = False
        Call protectInput
        Exit Sub
    End If

End If
  
  
'Shows or Hides Manual PD depending on the DFS
If Target.Address = "$E$10:$F$10" Or Target.Address = "$E$10" Then
    If Worksheets("New Input Mask").Range("E10").value = "" Then
        Call unprotectInput
        Worksheets("New Input Mask").Rows("12:13").Hidden = False
        Call protectInput
    Else
        Call unprotectInput
        Worksheets("New Input Mask").Rows("12:13").Hidden = True
        Call protectInput
    End If
End If
  
'Changes the repricing term value to the credit term selection
If Target.Address = "$H$47" Then
    Worksheets("New Input Mask").Range("H49").value = Worksheets("New Input Mask").Range("H47").value
End If
  
'Updates the Labels where the Deal Currency is shown
If Target.Address = "$D$27" Then
    If Left(Worksheets("New Input Mask").Range("Q19").value, 1) = "A" Then
        Worksheets("New Input Mask").Range("Q19").value = Worksheets("Index").Range("Aa4").value
    End If
'    If ComboBox1.value <> "%" Then
    If InStr(ComboBox1.value, "%") <> 1 Then
        ComboBox1.value = Left(Worksheets("INDEX").Range("Deal_Currency"), 3)
    End If
'    If Left(ComboBox2.value, 1) <> "%" Then
    If InStr(ComboBox2.value, "%") <> 1 Then
        ComboBox2.value = Left(Worksheets("INDEX").Range("Deal_Currency"), 3)
    End If
End If

'Deletes Benchmark after change of Interest Type
If Target.Address = "$E$49" Then
    Worksheets("Index").Range("B262").value = ""
    Worksheets("Index").Range("B263").value = ""
End If

If Target.Address = "$H$54" Then
    If CDbl(Target.value) < 0 Then
        If [Country_Short] = "SVK" Or [Country_Short] = "CZE" Then
            
        Else
            MsgBox "Please enter a valid Customer/Buy rate between 0 and 100"
            Range("$H$54").value = ""
        End If
        End If
End If

'Changes bank Branch flag in case product has been chosen
If Target.Address = "$E$25" Then
    If InStr(1, Worksheets("Index").Range("Financial_Product_Type").value, "Bank Branch") Or InStr(1, Target, "Bank Branch") Then
        Worksheets("Index").Range("Is_Bank_Branch_deal").value = 1
    Else
        Worksheets("Index").Range("Is_Bank_Branch_deal").value = 0
    End If
End If

'Sets Guaranteed RV to 100% for floorplan business
If Target.Address = "$E$25" Then
     If InStr(1, UCase(Worksheets("Index").Range("Product_Risk_Class")), "FLOORPLAN") Or InStr(1, UCase(Target), "FLOORPLAN") Then
        If Worksheets("New Input Mask").Range("E45").Locked = False Then
            Worksheets("New Input Mask").Range("E45").value = 100
            ComboBox2.ListIndex = 0
            Worksheets("New Input Mask").Unprotect Password:="Blattschutz"
            Worksheets("New Input Mask").Range("E45").Locked = True
            Worksheets("New Input Mask").Range("E45").Interior.ColorIndex = 22
            Worksheets("New Input Mask").Protect Password:="Blattschutz"
        End If
    Else
        Worksheets("New Input Mask").Unprotect Password:="Blattschutz"
        Worksheets("New Input Mask").Range("E45").Interior.ColorIndex = 2
        Worksheets("New Input Mask").Range("E45").value = ""
        Worksheets("New Input Mask").Range("E45").Locked = False
        Worksheets("New Input Mask").Protect Password:="Blattschutz"
     End If

End If

'Lock Manual PLF cell for Italy Retail

If Target.Address = "$E$8" And [Country_Short] = "ITA" Then
Application.EnableEvents = False
    If Worksheets("Index").Range("Borrower_Type").value = "Retail Private" _
    Or Worksheets("Index").Range("Borrower_Type").value = "Retail Small Business" Then
        Call unprotectInput
        Worksheets("New Input Mask").Range("E12:F12").Formula = "=VLOOKUP(Depreciation_Curve,IF(Company=""(ITA) Mercedes-Benz Financial Services Italia SpA"",Local_Sheet!P8:Q34,Local_Sheet!R6:S34),2,FALSE)"
        Worksheets("New Input Mask").Range("E12:F12").Locked = True
        Worksheets("New Input Mask").Range("E12").Interior.Color = RGB(175, 178, 180)
        Call protectInput
    Else
        Call unprotectInput
        Worksheets("New Input Mask").Range("E12:F12").Locked = False
        Worksheets("New Input Mask").Range("E12:F12").value = ""
        Worksheets("New Input Mask").Range("E12").Interior.Color = RGB(255, 255, 255)
        Call protectInput
    End If
Application.EnableEvents = True
End If


'end of lock

End Sub








--- Macro File: mdlDataload.bas ---
Attribute VB_Name = "mdlDataload"
'sub to load data from data entities to index sheet
'global constants from MDL_global will be used to determine the columns where information and parameter are stored
'intUpdateType
'1: Complete load
'2: Only Interest and Spreads will be loaded
Public Sub dataload(intUpdateType As Integer)

Call unprotectInput

Dim intEntity_Row As Integer
Dim wksIndex As Worksheet
Dim wksData_Entities As Worksheet
Dim i As Integer
Dim j As Integer
Dim a As Double
Dim b As Double

Set wksIndex = Sheets("Index")
Set wksData_Entities = Sheets("Data_Entities")

'Determines the position of the selected Entity within the Data Entities Sheet

intEntity_Row = fct_entity_data_position()

If intEntity_Row = 0 Then
    MsgBox "No Entity Data available"
    Call protectInput
    Exit Sub
End If

'Interest and Spreads
For i = 0 To 2
    wksIndex.Cells(86 + i, 24) = wksData_Entities.Cells(intEntity_Row + 2, posIntSpr_DE + i)

    For j = 0 To 29
        wksIndex.Cells(85 + j, 20 + i) = wksData_Entities.Cells(intEntity_Row + j, posIntSpr_DE + i)
    Next
    For j = 31 To 41
        wksIndex.Cells(85 + j, 20 + i) = wksData_Entities.Cells(intEntity_Row + j - 1, posIntSpr_DE + i)
    Next
Next

If intUpdateType <> 2 Then
    'Customer Type
    For i = 0 To 1
        For j = 0 To 24
           wksIndex.Cells(86 + j, 4 + i) = wksData_Entities.Cells(intEntity_Row + 1 + j, posCustBorType_DE + i)
        Next
    Next
    
    'OPEX Segments
    For i = 0 To 1
        For j = 0 To 65
           wksIndex.Cells(86 + j, 99 + i) = wksData_Entities.Cells(intEntity_Row + 1 + j, posOPEX_DE + i)
        Next
    Next
    
    'Product List
    For i = 0 To 2
        For j = 0 To 30
           wksIndex.Cells(86 + j, 10 + i) = wksData_Entities.Cells(intEntity_Row + 1 + j, posProduct_DE + i)
        Next
    Next
    
    'Internal Rating
    For i = 0 To 4
        For j = 0 To 11
           wksIndex.Cells(85 + j, 14 + i) = wksData_Entities.Cells(intEntity_Row + j, posIntPD_DE + i).Formula
        Next
    Next
    
    'Copy Depreciation Curves
    For i = 0 To 14
    
        wksIndex.Cells(85, 28 + i).value = wksData_Entities.Cells(intEntity_Row, posDepCurve_DE + i).value
        For j = 86 To 270
            wksIndex.Cells(j, 28 + i).value = ""
        Next
        wksIndex.Cells(86 + i, 26).value = wksData_Entities.Cells(intEntity_Row, posDepCurve_DE + i).value
        If wksIndex.Cells(85, 28 + i).value <> "" Then
            a = wksData_Entities.Cells(intEntity_Row + 2, posDepCurve_DE + i).value
            b = wksData_Entities.Cells(intEntity_Row + 3, posDepCurve_DE + i).value
            If wksData_Entities.Cells(intEntity_Row + 1, posDepCurve_DE + i).value = "a*e^bx" Then
                For j = 12 To 194
                    If (a * Exp(b * j / 12)) < 0 Then
                        wksIndex.Cells(74 + j, 28 + i).value = 0
                    Else
                        wksIndex.Cells(74 + j, 28 + i).value = a * Exp(b * j / 12)
                    End If
                Next
            ElseIf wksData_Entities.Cells(intEntity_Row + 1, posDepCurve_DE + i).value = "a*x+b" Then
                For j = 12 To 194
                    If (a * (j / 12) + b) < 0 Then
                        wksIndex.Cells(74 + j, 28 + i).value = 0
                    Else
                        wksIndex.Cells(74 + j, 28 + i).value = a * (j / 12) + b
                    End If
                Next
             ElseIf wksData_Entities.Cells(intEntity_Row + 1, posDepCurve_DE + i).value = "a*x^b" Then
                For j = 12 To 194
                    If (a * (j / 12) ^ b) < 0 Then
                        wksIndex.Cells(74 + j, 28 + i).value = 0
                    Else
                        wksIndex.Cells(74 + j, 28 + i).value = a * (j / 12) ^ b
                    End If
                Next
             Else
                For j = 12 To 194
                    If (a * WorksheetFunction.Application.WorksheetFunction.Ln(j / 12) + b) < 0 Then
                        wksIndex.Cells(74 + j, 28 + i).value = 0
                    Else
                        wksIndex.Cells(74 + j, 28 + i).value = a * WorksheetFunction.Application.WorksheetFunction.Ln(j / 12) + b
                    End If
                Next
            End If
        End If
    Next
    
    'Additional Parameter
    For i = 0 To 1
    wksIndex.Cells(86, 46 + i).value = wksData_Entities.Cells(intEntity_Row + 1, posAddPara_DE + i).value
    Next
    
    'Disposal Time
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE).value = "Flat" Then
        wksIndex.Cells(85, 51).value = "Flat"
    Else
        wksIndex.Cells(85, 51).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE).value
    End If
    
    For i = 0 To 1
        For j = 0 To 80
           wksIndex.Cells(86 + j, 50 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE - 1 + i).value
        Next
    Next
    
    'Remarketing Fix
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 2).value = "Flat" Then
        wksIndex.Cells(85, 54).value = "Flat"
    Else
        wksIndex.Cells(85, 54).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 2).value
    End If
    
    For i = 0 To 1
        For j = 0 To 67
           wksIndex.Cells(86 + j, 53 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 1 + i).value
        Next
    Next
    
    'Remarketing Variable
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 4).value = "Flat" Then
        wksIndex.Cells(85, 57).value = "Flat"
    Else
        wksIndex.Cells(85, 57).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 4).value
    End If
    
    For i = 0 To 1
        For j = 0 To 67
           wksIndex.Cells(86 + j, 56 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 3 + i).value
        Next
    Next
    
    'EC HC
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 6).value = "Flat" Then
        wksIndex.Cells(85, 60).value = "Flat"
    Else
        wksIndex.Cells(85, 60).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 6).value
    End If
    
    For i = 0 To 1
        For j = 0 To 19
           wksIndex.Cells(86 + j, 59 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 5 + i).value
        Next
    Next
    
    'Probability Cure
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 8).value = "Flat" Then
        wksIndex.Cells(85, 63).value = "Flat"
    Else
        wksIndex.Cells(85, 63).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 8).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 62 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 7 + i).value
        Next
    Next
    
    'Recovery Cure
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 10).value = "Flat" Then
        wksIndex.Cells(85, 66).value = "Flat"
    Else
        wksIndex.Cells(85, 66).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 10).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 65 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 9 + i).value
        Next
    Next
    
    'Probability Restructuring
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 12).value = "Flat" Then
        wksIndex.Cells(85, 69).value = "Flat"
    Else
        wksIndex.Cells(85, 69).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 12).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 68 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 11 + i).value
        Next
    Next
    
    'Recovery Restructuring
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 14).value = "Flat" Then
        wksIndex.Cells(85, 72).value = "Flat"
    Else
        wksIndex.Cells(85, 72).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 14).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 71 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 13 + i).value
        Next
    Next
    
    'EC OPR
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 16).value = "Flat" Then
        wksIndex.Cells(85, 75).value = "Flat"
    Else
        wksIndex.Cells(85, 75).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 16).value
    End If
    
    For i = 0 To 1
        For j = 0 To 9
           wksIndex.Cells(86 + j, 74 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 15 + i).value
        Next
    Next
    
    'EC Total
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 18).value = "Flat" Then
        wksIndex.Cells(85, 78).value = "Flat"
    Else
        wksIndex.Cells(85, 78).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 18).value
    End If
    
    For i = 0 To 1
        For j = 0 To 19
           wksIndex.Cells(86 + j, 77 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 17 + i).value
        Next
    Next
    
    'EC RVR
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 20).value = "Flat" Then
        wksIndex.Cells(85, 81).value = "Flat"
    Else
        wksIndex.Cells(85, 81).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 20).value
    End If
    
    For i = 0 To 1
        For j = 0 To 98
           wksIndex.Cells(86 + j, 80 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 19 + i).value
        Next
    Next
    
    'downturn
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 34).value = "Flat" Then
        wksIndex.Cells(85, 103).value = "Flat"
    Else
        wksIndex.Cells(85, 103).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 34).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 102 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 33 + i).value
        Next
    Next
    
    
    'shiftfactor
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 36).value = "Flat" Then
        wksIndex.Cells(85, 106).value = "Flat"
    Else
        wksIndex.Cells(85, 106).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 36).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 105 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 35 + i).value
        Next
    Next
    
    
    'LGD_Moc
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 38).value = "Flat" Then
        wksIndex.Cells(85, 109).value = "Flat"
    Else
        wksIndex.Cells(85, 109).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 38).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 108 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 37 + i).value
        Next
    Next
    
   
    
   
    
    'OneEC Field
    wksIndex.Cells(86, 111).value = wksData_Entities.Cells(intEntity_Row + 1, posOneEC).value
    
    
    
     'DCF_Moc
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 41).value = "Flat" Then
        wksIndex.Cells(85, 114).value = "Flat"
    Else
        wksIndex.Cells(85, 114).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 41).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 113 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 40 + i).value
        Next
    Next
    
    'QFactor
    
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 43).value = "Flat" Then
        wksIndex.Cells(85, 117).value = "Flat"
    Else
        wksIndex.Cells(85, 117).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 43).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 116 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 42 + i).value
        Next
    Next
    
    'R2
    
    If wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 43).value = "Flat" Then
        wksIndex.Cells(85, 120).value = "Flat"
    Else
        wksIndex.Cells(85, 120).value = "=" & wksData_Entities.Cells(intEntity_Row, posPLSPara_DE + 45).value
    End If
    
    For i = 0 To 1
        For j = 0 To 60
           wksIndex.Cells(86 + j, 119 + i).value = wksData_Entities.Cells(intEntity_Row + 1 + j, posPLSPara_DE + 44 + i).value
        Next
    Next
    
    
    
'PD_Moc
    For i = 0 To 4
        For j = 0 To 11
           wksIndex.Cells(85 + j, 14 + 109 + i) = wksData_Entities.Cells(intEntity_Row + j, posIntPD_DE + 76 + i).Formula
        Next
    Next
    
    
    
    
    
    'List Price Field
    wksIndex.Cells(86, 83).value = wksData_Entities.Cells(intEntity_Row + 1, posListPrice_DE).value
    
    'Start Value RV Curve
    wksIndex.Cells(86, 85).Formula = wksData_Entities.Cells(intEntity_Row + 1, posStartDPCur_DE).Formula
    
    'Opex Field
    wksIndex.Cells(86, 87).value = wksData_Entities.Cells(intEntity_Row + 1, posOPEXField).value
    
    'Opex Formula
    wksIndex.Cells(86, 89).Formula = wksData_Entities.Cells(intEntity_Row + 1, posOPEXFormula).Formula
    
    'PD Digits Allowed
    wksIndex.Cells(86, 91).value = wksData_Entities.Cells(intEntity_Row + 1, posPDDigitAllowed).value
    
    'Recovery Rate unsecured Portion
    wksIndex.Cells(86, 93).value = wksData_Entities.Cells(intEntity_Row + 1, posRecoveryUnsecured).value
    
    'CoR for Dealer Retail Calculation
    wksIndex.Cells(86, 95).value = wksData_Entities.Cells(intEntity_Row + 1, posCoR).value
    
    'Country Risk Add on by Country
    wksIndex.Cells(86, 97).value = wksData_Entities.Cells(intEntity_Row + 1, posEC_CTYR).value
    
    'PD Add for Bank Branch entity, CoR
    wksIndex.Range("Bank_Branch_PD_Addon_for_CoR").value = wksData_Entities.Cells(intEntity_Row + 1, posPDAddon1_CoR).value
    
    'PD Add for Bank Branch entity, EC
    wksIndex.Range("Bank_Branch_PD_Addon_for_EC").value = wksData_Entities.Cells(intEntity_Row + 1, posPDAddon2_EC).value
    
    'Effective Maturity for Blended Rorac
    wksIndex.Range("EffectiveMaturityBlended").value = wksData_Entities.Cells(intEntity_Row + 1, posEffMatBlended).value

    Application.EnableEvents = False
    
    'update of input fields with data from loaded entity
    Worksheets("New Input Mask").Select
    Worksheets("New Input Mask").Range("E8").value = wksIndex.Range("d86").value
    Worksheets("New Input Mask").Range("E19").value = wksIndex.Range("AB85").value
    Worksheets("New Input Mask").Range("E25").value = wksIndex.Range("J86").value
    Worksheets("New Input Mask").Range("G27").value = wksIndex.Range("CU86").value
    Worksheets("New Input Mask").Range("D27").value = wksIndex.Range("X86").value
    Worksheets("New Input Mask").Range("u19").value = wksIndex.Range("aw2").value
    
    If Worksheets("Index").Range("List_Price_Parameter") = "yes" Then
        Worksheets("New Input Mask").Cells(17, 5).Select
        Selection.Locked = False
        Worksheets("New Input Mask").Range("E17").Interior.Color = RGB(255, 255, 255)
    '    Worksheets("New Input Mask").Range("E17").Value = ""
        Worksheets("New Input Mask").CommandButton12.Visible = True
    Else
        Worksheets("New Input Mask").Cells(17, 5).Select
        Selection.Locked = True
        Worksheets("New Input Mask").Range("E17").Interior.Color = RGB(175, 178, 180)
        Worksheets("New Input Mask").Range("E17").value = ""
        Worksheets("New Input Mask").CommandButton12.Visible = False
    End If
    
    'Protect or Unprotect Opex Field depending on the Parameter for the specific Country
    If Worksheets("Index").Range("Opex_Parameter") = "yes" Then
        Worksheets("New Input Mask").Unprotect Password:="Blattschutz"
        Worksheets("New Input Mask").Cells(27, 7).Select
        Selection.Locked = False
        Worksheets("New Input Mask").Range("G27").Font.Color = RGB(38, 63, 106)
        Worksheets("New Input Mask").Range("G27").Interior.Color = RGB(255, 255, 255)
    Else
        Worksheets("New Input Mask").Unprotect Password:="Blattschutz"
        Worksheets("New Input Mask").Cells(27, 7).Select
        Selection.Locked = True
        Worksheets("New Input Mask").Range("G27").Interior.Color = RGB(175, 178, 180)
        Worksheets("New Input Mask").Range("G27").Font.Color = RGB(175, 178, 180)
    End If

    Worksheets("New Input Mask").Cells(8, 5).Select
    Worksheets("New Input Mask").Range("Q19").value = "Absolute in " & Worksheets("New Input Mask").Range("D27")
    Application.EnableEvents = True
End If

Call protectInput
Application.StatusBar = "Data for Entity successfully loaded"

End Sub


--- Macro File: DCF_Calculation.bas ---
Attribute VB_Name = "DCF_Calculation"
Option Explicit

Public Sub sub_CalcDCFExcel(prngYields As Range, plngComp As Long, pstrSW As String, pstrMM As String, padblDCF() As typDCF, pdblDate As Double, pintCurve As Integer, pintAnnualized As Integer)

    Dim ladblData() As Double
    Dim ladblDays() As Double
    Dim ladblDataSpreads() As Double
    Dim ladblDaysSpreads() As Double
    Dim lintFound As Integer
 
    Dim laTemp(-1 To cMaxMonths) As typCalcDCF
    
    Dim lintRun As Integer
    Dim lintCount As Integer
    
    '// read yield or spread data
    Call sub_ReadYieldsExcel(prngYields, plngComp, ladblData(), ladblDays(), pdblDate, pintCurve, pintAnnualized)
    
    '// insert neccessary dates into array
    For lintRun = plngComp To cMaxMonths 'Step plngComp
        laTemp(lintRun).Date = DateAdd("m", lintRun, pdblDate)
        laTemp(lintRun).Yield = -1
    Next lintRun
    
    '// initialize array with yield curve data
    For lintRun = 0 To UBound(ladblDays)
        lintCount = fct_DiffDays30(pdblDate + 1, ladblDays(lintRun) + 1)
        Select Case lintCount
            Case 2
                laTemp(-1).Yield = ladblData(lintRun) / 100
                laTemp(-1).Date = ladblDays(lintRun)
            Case 7
                laTemp(0).Yield = ladblData(lintRun) / 100
                laTemp(0).Date = ladblDays(lintRun)
            Case Else
                laTemp(lintCount / 30).Yield = ladblData(lintRun) / 100
                laTemp(lintCount / 30).Date = ladblDays(lintRun)
        End Select
    Next lintRun
    
    '// money market rates
    Call sub_CalcMoneyMarket(laTemp(), plngComp, pstrMM, pdblDate)
    
    '// swap rates
    Call sub_CalcSwapMarket(laTemp(), plngComp, pstrSW, pdblDate)
 
    '// save dcf in result array
    ReDim padblDCF(0)
    padblDCF(0).Date = pdblDate
    padblDCF(0).DCF = 1
    lintCount = 0
    For lintRun = -1 To cMaxMonths
        If laTemp(lintRun).DCF <> 0 Then
            lintCount = lintCount + 1
            ReDim Preserve padblDCF(lintCount)
            padblDCF(lintCount).Date = laTemp(lintRun).Date
            padblDCF(lintCount).DCF = laTemp(lintRun).DCF
        End If
    Next lintRun
    
End Sub
 


Public Sub sub_ReadYieldsExcel(prngYields As Range, ByVal pintComp As Integer, pdblYields() As Double, pdblDates() As Double, pdblStart As Double, pintCurve As Integer, pintAnnualized As Integer)

    Dim ldblHelp As Double
    Dim lintRun As Integer
    
    lintRun = 1
    
    '// one month
    If prngYields.Cells(1, 2 + pintCurve) <> 0 Then
        ldblHelp = prngYields.Cells(1, 2 + pintCurve)
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 1, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 2 months
    If prngYields.Cells(2, 2 + pintCurve) <> 0 Then
        ldblHelp = prngYields.Cells(2, 2 + pintCurve)
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 2, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 3 months
    If prngYields.Cells(3, 2 + pintCurve) <> 0 Then
        ldblHelp = prngYields.Cells(3, 2 + pintCurve)
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 3, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 6 months
    If prngYields.Cells(4, 2 + pintCurve) <> 0 Then
        ldblHelp = prngYields.Cells(4, 2 + pintCurve)
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 6, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 12 months
    If prngYields.Cells(5, 2 + pintCurve) <> 0 Then
        ldblHelp = prngYields.Cells(5, 2 + pintCurve)
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 12, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 2 years
    If prngYields.Cells(6, 2 + pintCurve) <> 0 Then
        If Not pintAnnualized Then
            ldblHelp = prngYields.Cells(6, 2 + pintCurve)
        Else
            ldblHelp = fct_YieldEff2Nom(prngYields.Cells(6, 2 + pintCurve), pintComp)
        End If
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 24, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 3 years
    If prngYields.Cells(7, 2 + pintCurve) <> 0 Then
        If Not pintAnnualized Then
            ldblHelp = prngYields.Cells(7, 2 + pintCurve)
        Else
            ldblHelp = fct_YieldEff2Nom(prngYields.Cells(7, 2 + pintCurve), pintComp)
        End If
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 36, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 4 years
    If prngYields.Cells(8, 2 + pintCurve) <> 0 Then
        If Not pintAnnualized Then
            ldblHelp = prngYields.Cells(8, 2 + pintCurve)
        Else
            ldblHelp = fct_YieldEff2Nom(prngYields.Cells(8, 2 + pintCurve), pintComp)
        End If
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 48, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 5 years
    If prngYields.Cells(9, 2 + pintCurve) <> 0 Then
        If Not pintAnnualized Then
            ldblHelp = prngYields.Cells(9, 2 + pintCurve)
        Else
            ldblHelp = fct_YieldEff2Nom(prngYields.Cells(9, 2 + pintCurve), pintComp)
        End If
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 60, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 7 years
    If prngYields.Cells(10, 2 + pintCurve) <> 0 Then
        If Not pintAnnualized Then
            ldblHelp = prngYields.Cells(10, 2 + pintCurve)
        Else
            ldblHelp = fct_YieldEff2Nom(prngYields.Cells(10, 2 + pintCurve), pintComp)
        End If
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 84, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 10 years
    If prngYields.Cells(11, 2 + pintCurve) <> 0 Then
        If Not pintAnnualized Then
            ldblHelp = prngYields.Cells(11, 2 + pintCurve)
        Else
            ldblHelp = fct_YieldEff2Nom(prngYields.Cells(11, 2 + pintCurve), pintComp)
        End If
        ReDim Preserve pdblYields(lintRun)
        pdblYields(lintRun) = ldblHelp
        ReDim Preserve pdblDates(lintRun)
        pdblDates(lintRun) = DateAdd("m", 120, pdblStart)
        lintRun = lintRun + 1
    End If
    
    '// 1 day, if is NULL fill with first available
    ldblHelp = pdblYields(1)
    pdblYields(0) = ldblHelp
    pdblDates(0) = pdblStart + 2

End Sub


Public Function fct_DCF(pdblDate As Double, pintType As Integer)

    '// pintType: 0 yield curve, 1 spread curve
    
    Call sub_CalcDCFExcel(Range("Interest_Spread_Curve"), _
                     Range("Compounding_Frequency"), _
                     Range("Day_Convention_SM"), _
                     Range("Day_Convention_MM"), _
                     gDCF, CDbl(Range("Calculation_Date")), _
                     pintType, Range("Annualized"))
                     
    fct_DCF = fct_CalcDCF(pdblDate, gDCF)
    Exit Function

End Function


Function fct_CalcDCF(pdblDate As Double, paDCF() As typDCF) As Double
 
    '// calculate day accurate discount factor by exponential interpolation
    
    Dim lintRun As Integer
    Dim ldblHelp As Double
    
    ldblHelp = paDCF(LBound(paDCF)).Date
    If pdblDate < ldblHelp Then
        fct_CalcDCF = 0
        Exit Function
    End If
    
    If pdblDate < paDCF(LBound(paDCF) + 1).Date Then
        If pdblDate = paDCF(LBound(paDCF)).Date Then
            fct_CalcDCF = paDCF(LBound(paDCF)).DCF
        Else
            fct_CalcDCF = fct_Linear(pdblDate - ldblHelp, 1, paDCF(LBound(paDCF) + 1).DCF, 0, paDCF(LBound(paDCF) + 1).Date - ldblHelp)
        End If
    End If
    
    lintRun = UBound(paDCF)
    If pdblDate >= paDCF(lintRun).Date Then
        fct_CalcDCF = 1 / ((1 / paDCF(lintRun).DCF) ^ ((pdblDate - ldblHelp) / (paDCF(lintRun).Date - ldblHelp)))
        Exit Function
    End If
    
    For lintRun = LBound(paDCF) + 1 To UBound(paDCF)
        If pdblDate >= paDCF(lintRun).Date And pdblDate < paDCF(lintRun + 1).Date Then
            If pdblDate = paDCF(lintRun).Date Then
                fct_CalcDCF = paDCF(lintRun).DCF
            Else
                fct_CalcDCF = fct_Exponential(pdblDate - ldblHelp, paDCF(lintRun).DCF, paDCF(lintRun + 1).DCF, paDCF(lintRun).Date - ldblHelp, paDCF(lintRun + 1).Date - ldblHelp)
            End If
            Exit Function
        End If
    Next lintRun
    
End Function



--- Macro File: mdlLGD_Generation.bas ---
Attribute VB_Name = "mdlLGD_Generation"
Option Explicit
Global arrLGD() As Variant
Global arrLGD_Generation(0 To 239, 0 To 21)

Function fctLGD_Generation(datePayout_Date As Date, _
                            intPayment_Frequency As Integer, _
                            dateFirst_Instalment_Date_Input As Date, _
                            arrSkip_Months(), _
                            intEAD_Adjustment_Factor As Integer, _
                            dblInititial_Direct_Cost As Double, _
                            dblSubsidies As Double, _
                            intCredit_Term As Integer, _
                            LiqRunoff() As typRunoff, _
                            strNew_Used As String, _
                            strDepreciation_Curve As String, _
                            arrDepreciation_Curve_Table(), _
                            intAge_of_used_Vehicles As Integer, _
                            dblMSRP As Double, _
                            intDisposal_Time As Integer, _
                            dblRemarketing_Cost_Fix As Double, _
                            intNumber_Of_Vehicles As Integer, _
                            dblRemarketing_Cost_Var As Double, _
                            strAdd_Coll_Type As String, dblAdd_Coll As Double, _
                            dblAdd_Coll2 As Double, _
                            dblProb_Cure As Double, _
                            dblRec_Cure As Double, _
                            dblProb_Restr As Double, _
                            dblRec_Restr As Double, strUS_OL As String, dblSales_Price As Double, _
                            dblManual_LGD As Double, lContract As typContract, arrNew_Credit_Runoff() As Variant, intRepricing_Term As Integer, arrResults() As typResults) As Variant()


Dim i As Long
Dim j As Long
Dim k As Long
Dim dateCash_Flow_Date As Date
Dim dateCash_Flow_Date_Current_Period As Date
Dim dateCash_Flow_Date_Pre_Period As Date
Dim dateFirst_Instalment_Date As Date
Dim dblAmortization_IDC_Subsidies_Period_0 As Double
Dim dblAmortization_IDC_Subsidies_Pre_Period As Double
Dim intPeriod_Counter_excl_Grace_Period_Pre_Period As Integer
Dim intPeriod_Counter_excl_Grace_Period_Current_Period As Integer
Dim arrLGD_Results(0 To 132, 0 To 3)
Dim intDate_is_Found  As Integer
Dim dblOutstanding_Amount_temp As Double
Dim dblIDC_Subsidies_temp As Double
Dim rngLGD As Range
Dim wksLGD As Worksheet
Dim intLoopTo As Integer
Dim wksIndex As Worksheet
Dim cal_lgd_remarketing As Double

Set wksLGD = Sheets("LGD")
Set rngLGD = wksLGD.Range("Mdl_LGD")

If dateFirst_Instalment_Date_Input = #12:00:00 AM# Or _
dateFirst_Instalment_Date_Input < DateAdd("m", intPayment_Frequency, datePayout_Date + 1) - 1 Then
    dateFirst_Instalment_Date = DateAdd("m", intPayment_Frequency, datePayout_Date + 1) - 1
Else
    dateFirst_Instalment_Date = dateFirst_Instalment_Date_Input
End If


j = 0

If [Manual_CF_Flag] = 1 Then
intLoopTo = intArray_Limit
Else
intLoopTo = intArray_Limit / intPayment_Frequency
End If


For i = 0 To intLoopTo
'--------------------------------------------------------------------------------------------------
    '#1 Cash Flow Date
    If i = 0 Then
        arrLGD_Generation(j, 1) = datePayout_Date
    Else
        dateCash_Flow_Date_Pre_Period = arrLGD_Generation(j - 1, 1)
        arrLGD_Generation(j, 1) = fctCash_Flow_Date(intPayment_Frequency, _
                                                                 dateCash_Flow_Date_Pre_Period, _
                                                                 dateFirst_Instalment_Date, _
                                                                 arrSkip_Months())
    End If
    '#1 Cash Flow Date
'--------------------------------------------------------------------------------------------------
    '#0 Period Counter excl Grace Period
    If i = 0 Then
        arrLGD_Generation(j, 0) = 0
    Else
        dateCash_Flow_Date_Pre_Period = arrLGD_Generation(j - 1, 1)
        dateCash_Flow_Date_Current_Period = arrLGD_Generation(j, 1)
        intPeriod_Counter_excl_Grace_Period_Pre_Period = arrLGD_Generation(j - 1, 0)
        
        arrLGD_Generation(j, 0) = _
        fctPeriod_Counter_excl_Grace_Period(dateCash_Flow_Date_Pre_Period, _
                                            dateCash_Flow_Date_Current_Period, _
                                            dateFirst_Instalment_Date, _
                                            intPeriod_Counter_excl_Grace_Period_Pre_Period, _
                                            intPayment_Frequency)
    End If
    '#0 Period Counter excl Grace Period
'--------------------------------------------------------------------------------------------------
    '#2 Cash Flow Date II
        dateCash_Flow_Date = arrLGD_Generation(j, 1)
        arrLGD_Generation(j, 2) = fctCash_Flow_DateII(dateCash_Flow_Date, WorksheetFunction.Min(-intEAD_Adjustment_Factor, -intPayment_Frequency), intPayment_Frequency, datePayout_Date, j)
    '#2 Cash Flow Date II
'--------------------------------------------------------------------------------------------------
    '#3 Amortization IDC/Subsidies
    If dblInititial_Direct_Cost - dblSubsidies = 0 Or i = 0 Then
        arrLGD_Generation(j, 3) = dblInititial_Direct_Cost - dblSubsidies
        Else
        arrLGD_Generation(j, 3) = lContract.LiqRunoff(i + 1).NBV - arrNew_Credit_Runoff(i + 1)
    End If
    '#3 Amortization IDC/Subsidies
'--------------------------------------------------------------------------------------------------
    '#4 Outstanding Amount - Carstas Output
    If dblInititial_Direct_Cost - dblSubsidies <> 0 Then
    arrLGD_Generation(j, 4) = lContract.LiqRunoff(i + 1).NBV - arrLGD_Generation(j, 3)
    Else
'        If intRepricing_Term < intCredit_Term And [Interest_Type] <> "Fix" Then
'        arrLGD_Generation(j, 4) = arrResults(i).CreditRunOff
'        Else
        arrLGD_Generation(j, 4) = lContract.LiqRunoff(i + 1).NBV
'        End If
    End If
    '#4 Outstanding Amount - Carstas Output
'--------------------------------------------------------------------------------------------------
    '#5 EAD (outstanding amount incl overdue payments)
    intDate_is_Found = 0
    If i = intPayment_Frequency Then
        dblOutstanding_Amount_temp = arrLGD_Generation(0, 4)
    End If
    
    If i = 0 Then
        arrLGD_Generation(j, 5) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrLGD_Generation(j, 0) = intCredit_Term Then
                arrLGD_Generation(j, 5) = 0
            Else
                If arrLGD_Generation(j, 1) < DateAdd("m", intEAD_Adjustment_Factor, arrLGD_Generation(0, 1) + 1) - 1 Then
                    If arrLGD_Generation(0, 4) > 0 Then
                        arrLGD_Generation(j, 5) = arrLGD_Generation(0, 4)
                    Else
                        arrLGD_Generation(j, 5) = 0
                    End If
                Else
                    For k = 0 To intArray_Limit
                        If arrLGD_Generation(k, 1) = arrLGD_Generation(j, 2) Then
                        intDate_is_Found = 1
                            If arrLGD_Generation(k, 4) > 0 Then
                                arrLGD_Generation(j, 5) = arrLGD_Generation(k, 4)
                                dblOutstanding_Amount_temp = arrLGD_Generation(k, 4)
                            Else
                                arrLGD_Generation(j, 5) = 0
                            End If
                        Exit For
                        End If
                    Next k
                    If intDate_is_Found = 0 Then
                        arrLGD_Generation(j, 5) = dblOutstanding_Amount_temp
                    End If
                End If
            End If
        Else
            If arrLGD_Generation(j, 0) > intCredit_Term Then
                arrLGD_Generation(j, 5) = 0
            Else
                If arrLGD_Generation(j, 1) < DateAdd("m", intEAD_Adjustment_Factor, arrLGD_Generation(0, 1) + 1) - 1 Then
                    If arrLGD_Generation(0, 4) > 0 Then
                        arrLGD_Generation(j, 5) = arrLGD_Generation(0, 4)
                    Else
                        arrLGD_Generation(j, 5) = 0
                    End If
                Else
                    For k = 0 To intArray_Limit
                        If arrLGD_Generation(k, 1) = arrLGD_Generation(j, 2) Then
                            intDate_is_Found = 1
                            If arrLGD_Generation(k, 4) > 0 Then
                                arrLGD_Generation(j, 5) = arrLGD_Generation(k, 4)
                                dblOutstanding_Amount_temp = arrLGD_Generation(k, 4)
                            Else
                                arrLGD_Generation(j, 5) = 0
                            End If
                            Exit For
                        End If
                    Next k
                    If intDate_is_Found = 0 Then
                        arrLGD_Generation(j, 5) = dblOutstanding_Amount_temp
                    End If
                End If
            End If
        End If
    End If
    '#5 EAD (outstanding amount incl overdue payments)
'--------------------------------------------------------------------------------------------------
    '#6 Value of Colateral % at Default Date
    
            If strNew_Used = "New" Then
            'Debug.Print LBound(arrDepreciation_Curve_Table, 2); UBound(arrDepreciation_Curve_Table, 2)
                For k = LBound(arrDepreciation_Curve_Table, 1) To UBound(arrDepreciation_Curve_Table, 1)
                'rngDepreciation_Curve_Table
                'Depreciation_Curve_Table als Array übergeben
                    If arrDepreciation_Curve_Table(k, LBound(arrDepreciation_Curve_Table, 2)) = arrLGD_Generation(j, 0) Then
                        arrLGD_Generation(j, 6) = arrDepreciation_Curve_Table(k, UBound(arrDepreciation_Curve_Table, 2)) / 100
                        Exit For
                    End If
                Next k
            Else
                For k = LBound(arrDepreciation_Curve_Table, 1) To UBound(arrDepreciation_Curve_Table, 1)
                    If arrDepreciation_Curve_Table(k, LBound(arrDepreciation_Curve_Table, 2)) = arrLGD_Generation(j, 0) + intAge_of_used_Vehicles Then
                        arrLGD_Generation(j, 6) = arrDepreciation_Curve_Table(k, UBound(arrDepreciation_Curve_Table, 2)) / 100
                        Exit For
                    End If
                Next k
            End If
        
    
    '#6 Value of Colateral % at Default Date
'--------------------------------------------------------------------------------------------------
    '#7 Value of Colateral abs at Default Date
    If i = 0 Then
        arrLGD_Generation(j, 7) = 0
    Else
        arrLGD_Generation(j, 7) = dblMSRP * arrLGD_Generation(j, 6)
    End If
    '#7 Value of Colateral abs at Default Date
'--------------------------------------------------------------------------------------------------
    '#8 Value of Colateral % at Liquidation Date
    If i = 0 Then
        arrLGD_Generation(j, 8) = 0
    Else
        If arrLGD_Generation(j, 5) = 0 Then
            arrLGD_Generation(j, 8) = 0
        Else
            If strNew_Used = "New" Then
                For k = LBound(arrDepreciation_Curve_Table, 1) To UBound(arrDepreciation_Curve_Table, 1)
                    If arrDepreciation_Curve_Table(k, LBound(arrDepreciation_Curve_Table, 2)) = arrLGD_Generation(j, 0) + intDisposal_Time Then
                        arrLGD_Generation(j, 8) = arrDepreciation_Curve_Table(k, UBound(arrDepreciation_Curve_Table, 2)) / 100
                        Exit For
                    End If
                Next k
            Else
                For k = LBound(arrDepreciation_Curve_Table, 1) To UBound(arrDepreciation_Curve_Table, 1)
                    If arrDepreciation_Curve_Table(k, LBound(arrDepreciation_Curve_Table, 2)) = arrLGD_Generation(j, 0) + _
                    intAge_of_used_Vehicles + intDisposal_Time Then
                        arrLGD_Generation(j, 8) = arrDepreciation_Curve_Table(k, UBound(arrDepreciation_Curve_Table, 2)) / 100
                        Exit For
                    End If
                Next k
            End If
        End If
    End If
    '#8 Value of Colateral % at Liquidation Date
'--------------------------------------------------------------------------------------------------
    '#9 Value of Colateral abs at Liquidation Date
    If i = 0 Then
        arrLGD_Generation(j, 9) = 0
    Else
        arrLGD_Generation(j, 9) = dblMSRP * arrLGD_Generation(j, 8)
    End If
    '#9 Value of Colateral abs at Liquidation Date
'--------------------------------------------------------------------------------------------------
    '#10 Remarketing Cost Fix
    If i = 0 Then
        arrLGD_Generation(j, 10) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrLGD_Generation(j, 0) = intCredit_Term Then
                arrLGD_Generation(j, 10) = 0
            Else
                arrLGD_Generation(j, 10) = dblRemarketing_Cost_Fix '* intNumber_Of_Vehicles
            End If
        Else
            If arrLGD_Generation(j, 0) > intCredit_Term Then
                arrLGD_Generation(j, 10) = 0
            Else
                arrLGD_Generation(j, 10) = dblRemarketing_Cost_Fix '* intNumber_Of_Vehicles
            End If
        End If
    End If
    '#10 Remarketing Cost Fix
'--------------------------------------------------------------------------------------------------
    '#11 Remarketing Cost Variable
    If i = 0 Then
        arrLGD_Generation(j, 11) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrLGD_Generation(j, 0) = intCredit_Term Then
                arrLGD_Generation(j, 11) = 0
            Else
                arrLGD_Generation(j, 11) = dblRemarketing_Cost_Var * arrLGD_Generation(j, 9)
            End If
        Else
            If arrLGD_Generation(j, 0) > intCredit_Term Then
                arrLGD_Generation(j, 11) = 0
            Else
                arrLGD_Generation(j, 11) = dblRemarketing_Cost_Var * arrLGD_Generation(j, 9)
            End If
        End If
    End If
    '#11 Remarketing Cost Variable
'--------------------------------------------------------------------------------------------------
    '#12 Additional Cost/Proceeds
   
    If i = 0 Then
        arrLGD_Generation(j, 12) = 0
    Else
        If arrLGD_Generation(j, 0) > intCredit_Term Then
                arrLGD_Generation(j, 12) = 0
            Else
                If strAdd_Coll_Type = 3 Then
                    If [Add_Coll_Type_2] <> "Asset" Then
                    arrLGD_Generation(j, 12) = -dblAdd_Coll
                    Else
                    arrLGD_Generation(j, 12) = -dblAdd_Coll - dblAdd_Coll2
                    End If
                ElseIf strAdd_Coll_Type = 1 Then
                If [Add_Coll_Type_2] <> "Asset" Then
                    arrLGD_Generation(j, 12) = -dblAdd_Coll
                    Else
                If dblManual_LGD < 0 Then
                    arrLGD_Generation(j, 12) = -dblAdd_Coll - dblAdd_Coll2 * arrLGD_Generation(j, 9) / 100
                    Else
                    arrLGD_Generation(j, 12) = -dblAdd_Coll - ((dblAdd_Coll2 * arrLGD_Generation(j, 5) * (1 - dblManual_LGD)) / 100)
                End If
                End If
                ElseIf strAdd_Coll_Type = 2 Then
                If [Add_Coll_Type_2] <> "Asset" Then
                    arrLGD_Generation(j, 12) = -dblAdd_Coll
                    Else
                If dblManual_LGD < 0 Then
                    arrLGD_Generation(j, 12) = -dblAdd_Coll - dblAdd_Coll2 * (arrLGD_Generation(j, 5) - arrLGD_Generation(j, 9) + arrLGD_Generation(j, 10) + arrLGD_Generation(j, 11)) / 100
                    Else
                    arrLGD_Generation(j, 12) = -dblAdd_Coll - ((arrLGD_Generation(j, 5) * dblManual_LGD * dblAdd_Coll2) / 100)
                End If
                End If
                ElseIf strAdd_Coll_Type = 4 Then
                    arrLGD_Generation(j, 12) = -dblAdd_Coll
                ElseIf strAdd_Coll_Type = 5 Then
                If [Add_Coll_Type_2] <> "Asset" Then
                    arrLGD_Generation(j, 12) = -dblAdd_Coll
                    Else
                    arrLGD_Generation(j, 12) = -dblAdd_Coll - dblAdd_Coll2 * arrLGD_Generation(j, 5) / 100
                End If
                End If
           '****A deposit does not impact the LGD in case of a fixed LGD
            If dblManual_LGD >= 0 Then
                arrLGD_Generation(j, 12) = arrLGD_Generation(j, 12) + dblAdd_Coll
            End If
           
           End If
        End If
'#12 Additional Cost/Proceeds
'--------------------------------------------------------------------------------------------------
    '#13 Estimated total recovery amount
    If i = 0 Then
        arrLGD_Generation(j, 13) = 0
    Else
        If dblManual_LGD < 0 Then
        arrLGD_Generation(j, 13) = WorksheetFunction.Max(0, arrLGD_Generation(j, 9) - arrLGD_Generation(j, 10) - _
                                   arrLGD_Generation(j, 11) - arrLGD_Generation(j, 12))
        Else
        arrLGD_Generation(j, 13) = WorksheetFunction.Max(0, ((1 - dblManual_LGD) * arrLGD_Generation(j, 5)) - arrLGD_Generation(j, 12))
        End If
    End If
    
    '#13 Estimated total recovery amount
'--------------------------------------------------------------------------------------------------
    '#14 Not yet amortized IDC/Subsidies
              
    intDate_is_Found = 0
    If i = intPayment_Frequency Then
        dblIDC_Subsidies_temp = arrLGD_Generation(0, 3)
    End If
    

    If i = 0 Then
        arrLGD_Generation(j, 14) = 0
    Else
        If arrLGD_Generation(j, 1) < DateAdd("m", intEAD_Adjustment_Factor, arrLGD_Generation(0, 1) + 1) - 1 Then
            arrLGD_Generation(j, 14) = arrLGD_Generation(0, 3)
        Else
            For k = 0 To intArray_Limit
                If arrLGD_Generation(k, 1) = arrLGD_Generation(j, 2) Then
                    intDate_is_Found = 1
                    arrLGD_Generation(j, 14) = arrLGD_Generation(k, 3)
                    dblIDC_Subsidies_temp = arrLGD_Generation(k, 3)
                    Exit For
                End If
            Next k
            If intDate_is_Found = 0 Then
                arrLGD_Generation(j, 14) = dblIDC_Subsidies_temp
            End If
        End If
    End If

    '#14 Not yet amortized IDC/Subsidies
'--------------------------------------------------------------------------------------------------
    '#15 Economic Loss
    If i = 0 Then
        arrLGD_Generation(j, 15) = 0
    Else
        If dblManual_LGD < 0 Then
            If strUS_OL = "Yes" Then
                If arrLGD_Generation(j, 0) = intCredit_Term Then
                    arrLGD_Generation(j, 15) = 0
                Else
                    If arrLGD_Generation(j, 5) - arrLGD_Generation(j, 13) + arrLGD_Generation(j, 14) > 0 Then
                        arrLGD_Generation(j, 15) = arrLGD_Generation(j, 5) - _
                                                   arrLGD_Generation(j, 13) + arrLGD_Generation(j, 14)
                    Else
                        arrLGD_Generation(j, 15) = 0
                    End If
                End If
            Else
                If arrLGD_Generation(j, 0) > intCredit_Term Then
                    arrLGD_Generation(j, 15) = 0
                Else
                    If arrLGD_Generation(j, 5) - arrLGD_Generation(j, 13) + arrLGD_Generation(j, 14) > 0 Then
                        arrLGD_Generation(j, 15) = arrLGD_Generation(j, 5) - _
                                                   arrLGD_Generation(j, 13) + arrLGD_Generation(j, 14)
                    Else
                        arrLGD_Generation(j, 15) = 0
                    End If
                End If
            End If
        Else
        arrLGD_Generation(j, 15) = arrLGD_Generation(j, 5) - arrLGD_Generation(j, 13)
        End If
    End If
    '#15 Economic Loss
'--------------------------------------------------------------------------------------------------
    '#16 Economic Loss in % of EAD Liquidation Scenario
    If i = 0 Then
        arrLGD_Generation(j, 16) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrLGD_Generation(j, 0) = intCredit_Term Or arrLGD_Generation(j, 5) = 0 Then
                arrLGD_Generation(j, 16) = 0
            Else
                If arrLGD_Generation(j, 15) / arrLGD_Generation(j, 5) > 0 Then
                    arrLGD_Generation(j, 16) = arrLGD_Generation(j, 15) / arrLGD_Generation(j, 5)
                Else
                    arrLGD_Generation(j, 16) = 0
                End If
            End If
        Else
            If arrLGD_Generation(j, 0) > intCredit_Term Or arrLGD_Generation(j, 5) = 0 Then
                arrLGD_Generation(j, 16) = 0
            Else
                If arrLGD_Generation(j, 15) / arrLGD_Generation(j, 5) > 0 Then
                    arrLGD_Generation(j, 16) = arrLGD_Generation(j, 15) / arrLGD_Generation(j, 5)
                Else
                    arrLGD_Generation(j, 16) = 0
                End If
            End If
        End If
    End If
    
    If i = 0 Then
        arrLGD_Generation(j, 21) = 0
    Else
        If strUS_OL = "Yes" Then
            If arrLGD_Generation(j, 0) = intCredit_Term Or arrLGD_Generation(j, 5) = 0 Then
                arrLGD_Generation(j, 21) = 0
            Else
                If arrLGD_Generation(j, 9) / arrLGD_Generation(j, 5) > 0 Then
                    arrLGD_Generation(j, 21) = WorksheetFunction.Max(0, 1 - arrLGD_Generation(j, 9) * Sheets("index").Range("Qfactor").value / arrLGD_Generation(j, 5)) * (1 - dblRec_Restr)
                Else
                    arrLGD_Generation(j, 21) = 0
                End If
            End If
        Else
            If arrLGD_Generation(j, 0) > intCredit_Term Or arrLGD_Generation(j, 5) = 0 Then
                arrLGD_Generation(j, 21) = 0
            Else
                If arrLGD_Generation(j, 9) / arrLGD_Generation(j, 5) > 0 Then
                    arrLGD_Generation(j, 21) = WorksheetFunction.Max(0, 1 - arrLGD_Generation(j, 9) * Sheets("index").Range("Qfactor").value / arrLGD_Generation(j, 5)) * (1 - dblRec_Restr)
                Else
                    arrLGD_Generation(j, 21) = 0
                End If
            End If
        End If
    End If
    
    
    
    
    
    
    '#16 Economic Loss in % of EAD Liquidation Scenario
'--------------------------------------------------------------------------------------------------
'#17 Economic Loss in % of EAD (LGD) Total   NewLGD
'cal_lgd_remarketing = Max(0, 1 - (arrLGD_Generation(j, 9) * wksIndex.Range("Qfactor").value) / (arrLGD_Generation(j, 5))) * (1 - dblRec_Restr)
'         cal_lgd_cure        = 1-r1;
'         cal_lgd_totalloss   = 1-r3;
 '        cal_lgd             = (p1*(1-cal_lgd_cure + p2*cal_lgd_remarketing + p3*cal_lgd_totalloss)*DCF;
  '       cal_lgd_MoC         = cal_lgd + downturn + MoC_A + MoC_C;
  '                          = (dblProb_Cure*(1 - dblRec_Cure) + dblProb_Restr *cal_lgd_remarketing + p3*cal_lgd_totalloss)*DCF;
            




    If i = 0 Then
        arrLGD_Generation(j, 17) = 0
    Else
            If dblManual_LGD < 0 Then
            
                If Sheets("index").Range("OneEC").value = "Y" Then
                        
                        
                       ' cal_lgd_remarketing = (WorksheetFunction.Max(0, (1 - (arrLGD_Generation(j, 9) * Sheets("index").Range("Qfactor").value)) / (arrLGD_Generation(j, 5)) * (1 - dblRec_Restr)))
                        'If (1 - (arrLGD_Generation(j, 9) * Sheets("index").Range("Qfactor").value) / (arrLGD_Generation(j, 5)) * (1 - dblRec_Restr)) < 0 Then cal_lgd_remarketing = 0
                         '   Else
                          '  cal_lgd_remarketing = (1 - (arrLGD_Generation(j, 9) * Sheets("index").Range("Qfactor").value) / (arrLGD_Generation(j, 5)) * (1 - dblRec_Restr))
                        'End If
                        arrLGD_Generation(j, 17) = (dblProb_Cure * (1 - dblRec_Cure) + dblProb_Restr * arrLGD_Generation(j, 21) + _
                                   ((1 - dblProb_Cure - dblProb_Restr) * (1 - Sheets("index").Range("Rec_3").value))) * (1 - Sheets("index").Range("recovery_unsecured").value) * Sheets("index").Range("DCF").value
            
                Else
                    arrLGD_Generation(j, 17) = (dblProb_Cure * (1 - dblRec_Cure) + dblProb_Restr * (1 - dblRec_Restr) + _
                                   ((1 - dblProb_Cure - dblProb_Restr) * arrLGD_Generation(j, 16))) * (1 - Sheets("index").Range("recovery_unsecured").value)
                End If
            Else
            arrLGD_Generation(j, 17) = arrLGD_Generation(j, 16)
            End If
    End If
'#17 Economic Loss in % of EAD (LGD) Total

j = j + 1
Next i

'-----Output
'-----Write EAD, LGD, LGD Liqui and Estimated Remarketing proceeds and amortization IDC and Subsidies

For i = 0 To j - 1
    arrLGD_Results(i, 0) = arrLGD_Generation(i, 5)  'EAD
    
    arrLGD_Results(i, 1) = arrLGD_Generation(i, 13) 'Estimated remarketing proceeds
    
    arrLGD_Results(i, 2) = arrLGD_Generation(i, 16) 'LGD Liqui
    arrLGD_Results(i, 3) = arrLGD_Generation(i, 17) 'Economic Loss in % of EAD (LGD) Total
    Worksheets("Index").Range("Mdl_Credit_Runoff")(i + 1, 3) = arrLGD_Generation(i, 3) 'amortization IDC and Subsidies


Next i
fctLGD_Generation = arrLGD_Results()

'-----Output

End Function





--- Macro File: Sheet6.cls ---
Attribute VB_Name = "Sheet6"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Frm_Ini.frm ---
Attribute VB_Name = "Frm_Ini"
Attribute VB_Base = "0{03A0EEDD-2A38-481D-A11C-CC75BD8E80FF}{D0CB7A48-6A31-4E07-8F2A-4DDCD08100B5}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
'Form to initalize tool for selected country

Private Sub ComboBox1_Change()

End Sub

'Sub to check if entered passwords fits to selected country
Private Sub CommandButton1_Click()

Dim pw As String
Dim t As Variant
Dim PW_coun As String
Dim bolFins As Boolean
Dim passwordrichtg As Boolean

passwordrichtig = False
pw = TextBox1.text

'check if a country was seleceted
If ComboBox1.text = "" Then
    MsgBox ("Please select a country")
    Exit Sub
End If

'Check if entered password fits to selected country
For k = 1 To 40
    If Worksheets("PW").Cells(k, 1).value = ComboBox1.text Then
    
        'ITA Special initialization
        Dim yesno As Integer
        If ComboBox1.text = "ITA" Then
            yesno = MsgBox("Do You want to hide Rorac Calculation details?", vbYesNo)
        End If
        
        'if password is correct then initialize the tool
        If pw = Worksheets("PW").Cells(k, 2) Then
            
            Call unprotectInput
            'Creates Entity List for the selected Country
            Dim j As Integer
            Application.EnableEvents = False
            j = 0
            Worksheets("New Input Mask").Range("E5").value = ComboBox1.text
            Worksheets("INDEX").Range("Company_List_For_Sel_Country").value = ""
            Worksheets("New Input Mask").Cells(5, 12) = ""
            'Find entities of selected country
            For i = 2 To 81
                If Worksheets("INDEX").Cells(i, 6) = Worksheets("New Input Mask").Cells(5, 5) Or Worksheets("INDEX").Cells(i, 3) = Worksheets("New Input Mask").Cells(5, 5) Then
                     Worksheets("INDEX").Cells(85 + j, 1) = Worksheets("INDEX").Cells(i, 2)
                     j = j + 1
                End If
            Next i
            Worksheets("New Input Mask").Cells(5, 12) = "Please choose an Entity"
            Worksheets("New Input Mask").Visible = True
            Worksheets("Portfolio").Visible = True
            Worksheets("Portfolio").Protect Password:="Blattschutz"
            Worksheets("New Input Mask").Activate
            Worksheets("New Input Mask").ScrollArea = "A1:Z100"
            Worksheets("New Input Mask").CommandButton11.Visible = False
            
            'ITA exception to hide P&L Calculation
            If yesno = 6 Then
                ActiveSheet.Range("M25:S45").Font.Color = vbWhite
                ActiveSheet.Range("V24:W43").Font.Color = vbWhite
            End If
            
            'Hide for Entities apart from Bank Branches ESP, UK and France the Manual MFR rate
            If ComboBox1.text <> "ESP" And ComboBox1.text <> "GBR" And ComboBox1.text <> "FRA" Then
              Worksheets("I_and_S").Activate
                Worksheets("I_and_S").Unprotect Password:="Blattschutz"
                               
                With Worksheets("I_and_S").Range("A31:D31").Borders(xlEdgeBottom)
                    .LineStyle = xlContinuous
                    .Weight = xlThick
                    .ColorIndex = xlAutomatic
                End With
                
                With Worksheets("I_and_S").Range("G31:K31").Borders(xlEdgeBottom)
                    .LineStyle = xlContinuous
                    .Weight = xlThick
                    .ColorIndex = xlAutomatic
                End With
                
                With Worksheets("I_and_S").Range("M31:Q31").Borders(xlEdgeBottom)
                    .LineStyle = xlContinuous
                    .Weight = xlThick
                    .ColorIndex = xlAutomatic
                End With
                
'***************Hide Row 32 if Bank Branch***************************************
                If Worksheets("I_and_S").Rows("32:32").Hidden = False Then
                    Worksheets("I_and_S").Rows("32:32").Hidden = True
                End If
                
                Worksheets("I_and_S").Protect Password:="Blattschutz"
                Worksheets("New Input Mask").Activate
            Else
'***************Unhide Row 32 if not Bank Branch*********************************
                If Worksheets("I_and_S").Rows("32:32").Hidden = True Then
                    Worksheets("I_and_S").Activate
                    Worksheets("I_and_S").Unprotect Password:="Blattschutz"
                    Worksheets("I_and_S").Rows("32:32").Hidden = False
                End If
                   
                Worksheets("I_and_S").Protect Password:="Blattschutz"
                Worksheets("New Input Mask").Activate
            End If
            
            Worksheets("Initialize").Visible = xlVeryHidden
            Worksheets("BOM Deals").Visible = True
            Worksheets("Cash Flow Analysis").Visible = True
            If ComboBox1.text = "AUT" Then
                Worksheets("Local_Sheet_AUT").Visible = True
                Else: Worksheets("Local_Sheet").Visible = True
             End If
            Worksheets("Manual_Cash_Flows").Visible = True
            ActiveWindow.DisplayHeadings = False
            Application.EnableEvents = True
            ActiveWindow.DisplayWorkbookTabs = False
            Call protectInput
            Worksheets("Index").[Initialized] = "Yes"
            Unload Me
            Exit Sub
        Else
            MsgBox ("Wrong Password, please try again.")
            TextBox1.text = ""
            Exit Sub
        End If
    End If
Next

MsgBox ("No Country information available. Please select another one")

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub



--- Macro File: mdlCash_Flow_Functions.bas ---
Attribute VB_Name = "mdlCash_Flow_Functions"
Option Explicit

'#0#
Public Function fctCash_Flow_Date(intPayment_Frequency As Integer, _
                                  dateCash_Flow_Date_Pre_Period As Date, _
                                  dateFirst_Instalment_Date As Date, _
                                  arrSkip_Months()) As Date
                                  
Dim intFlag As Integer
Dim i As Integer
Dim j As Integer



'In a former version of the tool there was to opportunity to select skip months in a multiple select box in the Optional Information Section of
'the input mask. This standard function has been replaced by the more powerful manual cash flow sheet which allows for unregular patterns of
'skiped months. The following code supports the old standard function and could be used in case if there is any demand for the old function. Because the [skip month flag]
'is currently always 0, the code will not be used.
If [Skip_Months_Flag] > 0 Then
    
   intFlag = 0
 
   For j = 0 To 0
   
        For i = LBound(arrSkip_Months()) To UBound(arrSkip_Months())
            If Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1) = arrSkip_Months(i) Then
                dateCash_Flow_Date_Pre_Period = DateAdd("m", intPayment_Frequency, _
                dateCash_Flow_Date_Pre_Period + 1) - 1
                intFlag = 1
                Exit For
            End If
        Next i
        
   
   
        If intFlag = 0 Then
            If Worksheets("Index").Range("Date_Case").value = "no" And dateFirst_Instalment_Date > 0 Then
            If dateFirst_Instalment_Date > DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1 Then
                fctCash_Flow_Date = dateFirst_Instalment_Date
            Else
                fctCash_Flow_Date = DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1
            End If
            Else
                If Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1) = 2 Then
                fctCash_Flow_Date = DateSerial(Year(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), 28)
                Else
                fctCash_Flow_Date = DateSerial(Year(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), Worksheets("Index").Range("Day_Payout").value)
                End If
            End If
        End If
       
        
        intFlag = 0
        
    Next j
    
End If

If Worksheets("Index").Range("Date_Case").value = "no" Then
    'In case there First Instalment Date in the Optional Information Section of the Input Mask is filled, this value has to be used as the first cash flow date after payout.
    'The formula: DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1 assures that also for Februar the next month is correctly determined. If you just add one month
    'to the 31.1.XX you could end up in March.
    If dateFirst_Instalment_Date > DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1 Then
    fctCash_Flow_Date = dateFirst_Instalment_Date
    Else
    fctCash_Flow_Date = DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1
    End If
Else
    If dateFirst_Instalment_Date > DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1 Then
        fctCash_Flow_Date = dateFirst_Instalment_Date
    Else
    'For date case = yes all February cash flows get either the 28th (no leap year) or 29th (leap year). If it's not a February the day of the payout date will be used (Range "Day_Payout" from Index sheet).
        If Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1) = 2 And fctIstSchaltjahr(Year(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1)) = True Then
        fctCash_Flow_Date = DateSerial(Year(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), 29)
        Else
            If Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1) = 2 And fctIstSchaltjahr(Year(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1)) = False Then
            fctCash_Flow_Date = DateSerial(Year(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), 28)
            Else
                fctCash_Flow_Date = DateSerial(Year(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), Month(DateAdd("m", intPayment_Frequency, dateCash_Flow_Date_Pre_Period + 1) - 1), Worksheets("Index").Range("Day_Payout").value)
            End If
        End If
    End If
End If

End Function

Public Function fctIstSchaltjahr(Jahr As Long) As Boolean
   If (Jahr Mod 4 = 0 And Jahr Mod 100 <> 0) Or _
    (Jahr Mod 400 = 0) Then fctIstSchaltjahr = True
End Function




'#1#
Public Function fctPeriod_Counter_excl_Grace_Period(dateCash_Flow_Date_Pre_Period As Date, _
                                                    dateCash_Flow_Date_Current_Period As Date, _
                                                    dateFirst_Instalment_Date As Date, _
                                                    intPeriod_Counter_excl_Grace_Period_Pre_Period As Integer, _
                                                    intPayment_Frequency As Integer) As Integer

If dateFirst_Instalment_Date = dateCash_Flow_Date_Current_Period Then
fctPeriod_Counter_excl_Grace_Period = intPayment_Frequency
Else
fctPeriod_Counter_excl_Grace_Period = Round(fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 30 _
                                      + intPeriod_Counter_excl_Grace_Period_Pre_Period, 0)
End If
End Function

'#2#
Public Function fctPeriod_Counter_incl_Grace_Period(intPeriod_Counter_excl_Grace_Period As Integer, _
                                                    intInterest_Only_Period As Integer) As Integer


If intPeriod_Counter_excl_Grace_Period - intInterest_Only_Period > 0 Then
    fctPeriod_Counter_incl_Grace_Period = intPeriod_Counter_excl_Grace_Period - intInterest_Only_Period
Else
    fctPeriod_Counter_incl_Grace_Period = 0
End If

End Function

'#3#
Public Function fctFactor(dblNOM_CR As Double, _
                          dateCash_Flow_Date_Pre_Period As Date, _
                          dateCash_Flow_Date_Current_Period As Date, _
                          dblFactor_Pre_Period As Double) As Double

'For each cash flow period a discount factor is calculated based on either the nominal customer rate or, for principal only periods, a Zero interest rate.
fctFactor = 1 / _
            (1 + dblNOM_CR * fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / _
            360) * dblFactor_Pre_Period
End Function

'#4#
Public Function fctIrregular_Instalment(strUS_OL As String, _
                                        intPeriod_Counter_excl_Grace_Period As Integer, _
                                        intCredit_Term As Integer, _
                                        intPayment_Frequency As Integer, _
                                        dblcontracted_RV As Double, _
                                        dateCash_Flow_Date As Date, _
                                        dateExtra_Ordinary_Payment_Date As Date, _
                                        dblExtra_Ordinary_Payment_Amount As Double) As Double
                                           
Dim dblNAF_RV_Balloon_Payments As Double
Dim dblExtra_Ordinary As Double


If (strUS_OL = "No" And intPeriod_Counter_excl_Grace_Period = intArray_Limit) Or _
(strUS_OL = "Yes" And intPeriod_Counter_excl_Grace_Period = intCredit_Term) Then
    dblNAF_RV_Balloon_Payments = dblcontracted_RV
Else
    dblNAF_RV_Balloon_Payments = 0
End If

'Initially the extraordinary payment function was part of the optional information section. There exactly one extraordinary payment could
'be entered. With the introduction of the MCF sheet this function was taken out of the optional information section because the MCF allows
'for the entry of multiple extraordinary payments at different cashflow dates.

dblExtra_Ordinary = 0

fctIrregular_Instalment = dblNAF_RV_Balloon_Payments + dblExtra_Ordinary

End Function

'#5#
Public Function fctIrregular_Interest(intPeriod_Counter_excl_Grace_Period As Integer, _
                                      intInterest_Only_Period As Integer, _
                                      dblNAF As Double, _
                                      dblNOM_CR As Double, _
                                      dateCash_Flow_Date_Pre_Period As Date, _
                                      dateCash_Flow_Date_Current_Period As Date, _
                                      dateCash_Flow_Date_Next_Period As Date, _
                                      strPayment_Mode As String) As Double

'Update the function so that interest only period also works with payment mode in advance
If strPayment_Mode = "In Arrears" And intPeriod_Counter_excl_Grace_Period > intInterest_Only_Period Or _
strPayment_Mode = "In Advance" And intPeriod_Counter_excl_Grace_Period >= intInterest_Only_Period Then
    fctIrregular_Interest = 0
Else
'If interest only periods occur at the very beginning of a contract the Financed Amount (dblNAF) can be used to calculate irregular
'interest because there has been no amortization of the outstanding yet.

    If strPayment_Mode = "In Arrears" Then
        fctIrregular_Interest = dblNAF * dblNOM_CR * _
                                fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360
    Else
        fctIrregular_Interest = (dblNAF * dblNOM_CR * fct_DiffDays30(dateCash_Flow_Date_Current_Period, dateCash_Flow_Date_Next_Period) / 360) _
                                / (1 + dblNOM_CR * fct_DiffDays30(dateCash_Flow_Date_Current_Period, dateCash_Flow_Date_Next_Period) / 360)
    End If
End If

End Function

'#6#
Public Function fctRegular_Payment_Excl_Payment_Mode(strPayment_Mode As String, _
                                                     dateCash_Flow_Date As Date, _
                                                     dateFirst_Instalment_Date As Date, _
                                                     intPeriod_Counter_excl_Grace_Period As Integer, _
                                                     intCredit_Term As Integer, _
                                                     intPayment_Frequency As Integer, _
                                                     intInterest_Only_Period As Integer, _
                                                     strUS_OL As String, _
                                                     dblLast_Instalment As String) As Integer
If strUS_OL = "No" Then
    If dateCash_Flow_Date < dateFirst_Instalment_Date Or _
       intPeriod_Counter_excl_Grace_Period > intCredit_Term Or _
       intPeriod_Counter_excl_Grace_Period = intInterest_Only_Period Or _
       intPeriod_Counter_excl_Grace_Period < intInterest_Only_Period Or _
       dblLast_Instalment = "no" And intPeriod_Counter_excl_Grace_Period = intCredit_Term Then
        fctRegular_Payment_Excl_Payment_Mode = 0
    Else
        fctRegular_Payment_Excl_Payment_Mode = 1
    End If
Else
    'For the special case of the combination of "in advance" payment mode and postponed payment (e.g. Portugal) the credit term is extended by 1
    'to have no regular payment at the date when the postponed ballon/rv is paid because the postponment only effects the ballon/rv payment and not
    'the regular payment.
    If dateCash_Flow_Date < dateFirst_Instalment_Date Or _
       strPayment_Mode = "In Advance" And dblLast_Instalment = "yes" And intPeriod_Counter_excl_Grace_Period = (intCredit_Term + 1) Or _
       intPeriod_Counter_excl_Grace_Period = intCredit_Term Or _
       intPeriod_Counter_excl_Grace_Period = intInterest_Only_Period Or _
       intPeriod_Counter_excl_Grace_Period < intInterest_Only_Period Then
        fctRegular_Payment_Excl_Payment_Mode = 0
    Else
        fctRegular_Payment_Excl_Payment_Mode = 1
    End If
End If

End Function

'#7#
Public Function fctRegular_Payment_Incl_Payment_Mode(strPayment_Mode As String, _
                                         dateCash_Flow_Date_Next_Period As Date, _
                                         datePayout_Date As Date, _
                                         intPayment_Frequency As Integer, _
                                         dateFirst_Instalment_Date_Input As Date, _
                                         dateFirst_Instalment_Date As Date, _
                                         intRegular_Payment_Excl_Payment_Mode_Current_Period As Integer, _
                                         intRegular_Payment_Excl_Payment_Mode_Next_Period, _
                                         i As Long) As Integer
'in France and Portugal the downpayment for in advance contract is handled like a first instalment; hence there is no regular instalment and
'the counter is set to 0
If i = 0 And ((Worksheets("Index").Range("Country_Short").value = "PRT" And strPayment_Mode = "In Advance") Or [France_First_Instalment_Flag] = "Yes") Then
fctRegular_Payment_Incl_Payment_Mode = 0
Exit Function
End If

If strPayment_Mode = "In Arrears" Or _
   ((dateCash_Flow_Date_Next_Period = dateFirst_Instalment_Date) And _
   (dateFirst_Instalment_Date_Input - datePayout_Date) > (intPayment_Frequency * 30)) Then
   fctRegular_Payment_Incl_Payment_Mode = intRegular_Payment_Excl_Payment_Mode_Current_Period
Else
   fctRegular_Payment_Incl_Payment_Mode = intRegular_Payment_Excl_Payment_Mode_Next_Period
End If

End Function

'#8#
Public Function fctRegular(intRegular_Payment_Incl_Payment_Mode As Integer) As Double

fctRegular = intRegular_Payment_Incl_Payment_Mode

End Function

'#9#
Public Function fctRegular_Payments(intRegular As Integer, _
                                    dblRegular_Payment As Double) As Double
fctRegular_Payments = intRegular * dblRegular_Payment

End Function

'#10#
Public Function fctTotal_Cash_Flow_excl_IDC_Subs(dblIrregular_Instalment As Double, _
                                                 dblIrregular_Interest As Double, _
                                                 dblRegular_Payments As Double) As Double

fctTotal_Cash_Flow_excl_IDC_Subs = dblIrregular_Instalment + dblIrregular_Interest + dblRegular_Payments

End Function

'#11#
Public Function fctTotal_Cash_Flow_incl_IDC_Subs(dblTotal_Cash_Flow_excl_IDC_Subs As Double) As Double

fctTotal_Cash_Flow_incl_IDC_Subs = dblTotal_Cash_Flow_excl_IDC_Subs

End Function

'#12#
Public Function fctRegular_Payments_variable(intRegular As Integer, _
                                    dblRegular_Payment_variable As Double) As Double
fctRegular_Payments_variable = intRegular * dblRegular_Payment_variable

End Function

'#13#
Public Function fctIrregular(dblIrregular_Instalment As Double) As Double

fctIrregular = dblIrregular_Instalment
End Function

'#14#
Public Function fctTotal_Principal_excl_Interest(strPayment_Mode As String, _
                                                 dblRegular_Principal_Current_Period As Double, _
                                                 dateCash_Flow_Date_Next_Period As Date, _
                                                 datePayout_Date As Date, _
                                                 intPayment_Frequency As Integer, _
                                                 dateFirst_Instalment_Date_Input As Date, _
                                                 dateFirst_Instalment_Date As Date, _
                                                 dblRegular_Principal_Next_Period As Double, _
                                                 dblIrregular As Double, _
                                                 i As Long) As Double
If i = 0 Then
    If strPayment_Mode = "In Arrears" Or _
   ((dateCash_Flow_Date_Next_Period = dateFirst_Instalment_Date) And _
   (dateFirst_Instalment_Date_Input - datePayout_Date) > (intPayment_Frequency + 30)) Then
        fctTotal_Principal_excl_Interest = dblRegular_Principal_Current_Period + dblIrregular
    Else
            fctTotal_Principal_excl_Interest = dblRegular_Principal_Current_Period + _
                                           dblIrregular + dblRegular_Principal_Next_Period
End If
Else
     If strPayment_Mode = "In Arrears" Or _
     ((dateFirst_Instalment_Date_Input - datePayout_Date) > (intPayment_Frequency + 30)) Then
     fctTotal_Principal_excl_Interest = dblRegular_Principal_Current_Period + _
                                           dblIrregular
                                           Else
        fctTotal_Principal_excl_Interest = dblRegular_Principal_Next_Period + _
                                           dblIrregular
    End If
   
End If
End Function

'#15#
Public Function fctOutstanding_Liquidity_Binding(intPeriod_Counter_excl_Grace_Period As Integer, _
                                                 intCredit_Term As Integer, _
                                                 intPeriod_Counter_incl_Grace_Period As Integer, _
                                                 intInterest_Only_Period As Integer, _
                                                 dblOutstanding_Liquidity_Binding_Pre_Period As Double, _
                                                 dblTotal_Principal_excl_Interest As Double, _
                                                 intPayment_Frequency As Integer, _
                                                 strUS_OL As String) As Double

If strUS_OL = "Yes" Then
    If intPeriod_Counter_excl_Grace_Period = intCredit_Term Or _
        intPeriod_Counter_incl_Grace_Period = intCredit_Term - intPayment_Frequency - intInterest_Only_Period Then
        fctOutstanding_Liquidity_Binding = 0
    Else
        If dblOutstanding_Liquidity_Binding_Pre_Period - dblTotal_Principal_excl_Interest > 0 Then
            fctOutstanding_Liquidity_Binding = dblOutstanding_Liquidity_Binding_Pre_Period - dblTotal_Principal_excl_Interest
        Else
            fctOutstanding_Liquidity_Binding = 0
        End If
    End If
Else

    If intPeriod_Counter_excl_Grace_Period > intCredit_Term Or _
        intPeriod_Counter_incl_Grace_Period = intCredit_Term - intInterest_Only_Period Then
        fctOutstanding_Liquidity_Binding = 0
    Else
        If dblOutstanding_Liquidity_Binding_Pre_Period - dblTotal_Principal_excl_Interest > 0 Then
            fctOutstanding_Liquidity_Binding = dblOutstanding_Liquidity_Binding_Pre_Period - dblTotal_Principal_excl_Interest
        Else
            fctOutstanding_Liquidity_Binding = 0
        End If
    End If
End If
End Function

'#16#
Public Function fctPrincipal_Interest_Binding(intPeriod_Counter_excl_Grace_Period As Integer, _
                                              intInterest_Type_num As Integer, _
                                              dblTotal_Principal_excl_Interest As Double, _
                                              dblOutstanding_Liquidity_Binding_Pre_Period As Double, _
                                              intPayment_Frequency As Integer) As Double

If intPeriod_Counter_excl_Grace_Period < intInterest_Type_num Then
    fctPrincipal_Interest_Binding = dblTotal_Principal_excl_Interest
Else
    If intInterest_Type_num > intPayment_Frequency Then
        If intPeriod_Counter_excl_Grace_Period >= intInterest_Type_num Then
            fctPrincipal_Interest_Binding = dblOutstanding_Liquidity_Binding_Pre_Period
        Else
            fctPrincipal_Interest_Binding = 0
        End If
    Else
        If intPeriod_Counter_excl_Grace_Period >= intPayment_Frequency Then
            fctPrincipal_Interest_Binding = dblOutstanding_Liquidity_Binding_Pre_Period
        Else
            fctPrincipal_Interest_Binding = 0
        End If
    End If
End If
End Function

'#17#
Public Function fctOutstanding_Interest_Binding(dblOutstanding_Interest_Binding_Pre_Period As Double, _
                                                dblPrincipal_Interest_Binding As Double) As Double

fctOutstanding_Interest_Binding = dblOutstanding_Interest_Binding_Pre_Period - dblPrincipal_Interest_Binding

End Function


'#18# = fctCash_Flow_Date

'#19#
Public Function fctTotal_Fix_Cash_Flows(strInterest_Type As String, _
                                        dblTotal_Cash_Flow_incl_IDC_Subs As Double, _
                                        dblTotal_Principal_excl_Interest As Double, _
                                        dblInititial_Direct_Cost As Double, _
                                        dblSubsidies As Double, _
                                        i As Long, _
                                        dblAmmortization_Method As Double)
If i = 0 Then
    If strInterest_Type = "Fix" Or (strInterest_Type <> "Fix" And dblAmmortization_Method = 2) Then
        fctTotal_Fix_Cash_Flows = dblTotal_Cash_Flow_incl_IDC_Subs
    Else
        fctTotal_Fix_Cash_Flows = dblTotal_Principal_excl_Interest - dblInititial_Direct_Cost + dblSubsidies
    End If
Else
    If strInterest_Type = "Fix" Or (strInterest_Type <> "Fix" And dblAmmortization_Method = 2) Then
        fctTotal_Fix_Cash_Flows = dblTotal_Cash_Flow_incl_IDC_Subs
    Else
        fctTotal_Fix_Cash_Flows = dblTotal_Principal_excl_Interest
    End If
End If
End Function

'#20#
Public Function fctTotal_Variable_Cash_Flows(strInterest_Type As String, _
                                             dblOutstanding_Interest_Binding_Pre_Period As Double, _
                                             dblNOM_CR As Double, _
                                             dateCash_Flow_Date_Pre_Period As Date, _
                                             dateCash_Flow_Date_Current_Period As Date, _
                                             strFlag As String, _
                                             dblAmmortization_Method As Double) As Double

If strInterest_Type = "Fix" Or (strInterest_Type <> "Fix" And strFlag = "no" And dblAmmortization_Method = 2) Then
    fctTotal_Variable_Cash_Flows = 0
Else
    If dblOutstanding_Interest_Binding_Pre_Period > 0 Then
        fctTotal_Variable_Cash_Flows = dblOutstanding_Interest_Binding_Pre_Period * dblNOM_CR * _
        fct_DiffDays30(dateCash_Flow_Date_Pre_Period, dateCash_Flow_Date_Current_Period) / 360
    Else
        fctTotal_Variable_Cash_Flows = 0
    End If
End If

End Function




--- Macro File: mdlCash_Flow_Generation.bas ---
Attribute VB_Name = "mdlCash_Flow_Generation"
Option Explicit
Global arrCF() As Variant
Global intArray_Limit As Integer
Global dblEffective_Maturity As Double
Global arrCash_Flow_Generation(0 To 1000, 0 To 20)
Global dblAct_Installment As Double
Global dblIni_NPV As Double
Global dblNPV As Double







Function fctCash_Flow_Generation(intPayment_Frequency As Integer, _
                            strInterest_Type As String, _
                            intCredit_Term As Integer, _
                            intInterest_Only_Period As Integer, _
                            datePayout_Date As Date, _
                            dblNOM_CR As Double, _
                            dblNAF As Double, _
                            dblSales_Price As Double, _
                            dblAdditional_Financed_Items As Double, _
                            dblDown_Payment As Double, _
                            dblcontracted_RV As Double, _
                            dblInititial_Direct_Cost As Double, _
                            dblSubsidies As Double, _
                            strPayment_Mode As String, _
                            strUS_OL As String, _
                            dateExtra_Ordinary_Payment_Date As Date, _
                            dblExtra_Ordinary_Payment_Amount As Double, _
                            dateFirst_Instalment_Date_Input As Date, _
                            arrSkip_Months(), _
                            intInterest_Type_num As Integer, _
                            dblLast_Instalment As String, _
                            strRORACTargetCase As String, _
                            lContract As typContract, _
                            arrCF() As Variant, laCurve() As typCurveInput, laSpreads() As typCurveInput, dblCalculation_Date As Double, strMM As String, strSW As String, intCompounding_Frequency As Integer, intAnnualized As Integer) As Variant()

Dim intPeriod_Counter_excl_Grace_Period As Integer
Dim intPeriod_Counter_incl_Grace_Period As Integer
Dim dateCash_Flow_Date_Pre_Period As Date
Dim dateCash_Flow_Date_Current_Period As Date
Dim intPeriod_Counter_excl_Grace_Period_Pre_Period As Integer
Dim dblFactor_Pre_Period As Double
Dim dateCash_Flow_Date_Next_Period As Date
Dim intRegular_Payment_Excl_Payment_Mode_Current_Period As Integer
Dim intRegular_Payment_Excl_Payment_Mode_Next_Period As Integer
Dim intRegular_Payment_Incl_Payment_Mode As Integer
Dim intRegular As Integer
Dim dblIrregular_Instalment As Double
Dim dblIrregular_Interest As Double
Dim dblRegular_Payments As Double
Dim dblTotal_Cash_Flow_excl_IDC_Subs As Double

Dim dblOutstanding_Liquidity_Binding_Pre_Period As Double
Dim dblTotal_Principal_excl_Interest As Double
Dim dblOutstanding_Interest_Binding_Pre_Period As Double
Dim dblPrincipal_Interest_Binding As Double
Dim dblTotal_Cash_Flow_incl_IDC_Subs As Double
Dim arrMan_Cash_Flow(0 To 120, 0 To 4)

Dim i As Long
Dim j As Long

Dim dblZaehler As Double
Dim dblZaehler_variable As Double
Dim dblNenner As Double
Dim dblNenner_variable As Double
Dim dateCash_Flow_Date As Date

Dim dateFirst_Instalment_Date As Date
Dim dblRegular_Payment As Double
Dim dblRegular_Payment_variable As Double
Dim dblSum_Irregular_Instalment As Double
Dim dblSum As Double
Dim blnExists As Boolean
Dim strFlag As String
Dim dblAmmortization_Method As Double
Dim dblAccPayment As Double
Dim counter As Integer
Dim intLoopTo As Integer
Dim arrTarget_CF As Variant
Dim wksIndex As Worksheet

counter = -1

If [Accelerated_Payment_Flag] = 1 Then
    dblAccPayment = [Start_Value_Acc_Payment]
End If

Iteration:

counter = counter + 1


blnExists = False
dblZaehler = 0
dblNenner = 0
dblSum_Irregular_Instalment = 0
dblSum = 0
dblIni_NPV = 0
dblNPV = 0



If dateFirst_Instalment_Date_Input = #12:00:00 AM# Or _
dateFirst_Instalment_Date_Input < DateAdd("m", intPayment_Frequency, datePayout_Date + 1) - 1 Then
    dateFirst_Instalment_Date = DateAdd("m", intPayment_Frequency, datePayout_Date + 1) - 1
Else
    dateFirst_Instalment_Date = dateFirst_Instalment_Date_Input
End If

If dateFirst_Instalment_Date_Input <> #12:00:00 AM# Then
    dateLast_Payment_Date = DateAdd("m", (intCredit_Term - intPayment_Frequency), dateFirst_Instalment_Date + 1) - 1
Else
    dateLast_Payment_Date = DateAdd("m", intCredit_Term, datePayout_Date + 1) - 1
End If

i = 2
j = 0

'Read manual Cash Flow from MCF Sheet
If [Manual_CF_Flag] = 1 Or [Accelerated_Payment_Flag] = 1 Then
    While Worksheets("Manual_Cash_Flows").Cells(i, 1) <> ""
        If Worksheets("Manual_Cash_Flows").Cells(i, 1) = "No" Or [Accelerated_Payment_Flag] = 1 Then
            'Cash Flow Date
            arrMan_Cash_Flow(j, 1) = Worksheets("Manual_Cash_Flows").Cells(i, 2)
            'Cash FLow Irregular Payment
            'Updated so that accelerated payment also works with payment mode in advance
            If [Accelerated_Payment_Flag] = 1 And arrMan_Cash_Flow(j, 1) <= [Accelerated_Payment_End] And strPayment_Mode = "In Arrears" And j > 0 Then
                arrMan_Cash_Flow(j, 2) = dblAccPayment
            ElseIf [Accelerated_Payment_Flag] = 1 And arrMan_Cash_Flow(j, 1) < [Accelerated_Payment_End] And strPayment_Mode = "In Advance" Then
                If j = 0 Then
                    arrMan_Cash_Flow(j, 2) = -dblNAF + dblAccPayment
                Else
                    arrMan_Cash_Flow(j, 2) = dblAccPayment
                End If
            Else
                arrMan_Cash_Flow(j, 2) = Worksheets("Manual_Cash_Flows").Cells(i, 3)
            End If
            'Regular Cash Flow Payment in Date?
            'Updated so that accelerated payment function also works with last installment
            If [Accelerated_Payment_Flag] = 1 And i > 2 Then
                If dblLast_Instalment = "no" And j = intCredit_Term _
                Or strPayment_Mode = "In Advance" And dblLast_Instalment = "no" And j = (intCredit_Term - 1) _
                Or strPayment_Mode = "In Advance" And dblLast_Instalment = "yes" And j = intCredit_Term Then
                    arrMan_Cash_Flow(j, 4) = 0
                Else
                    arrMan_Cash_Flow(j, 4) = 1
                End If
            Else
                If Worksheets("Manual_Cash_Flows").Cells(i, 5) = "Principal and Interest" Or Worksheets("Manual_Cash_Flows").Cells(i, 5) = "Principal Only" Then
                    arrMan_Cash_Flow(j, 4) = 1
                Else
                    arrMan_Cash_Flow(j, 4) = 0
                End If
            End If
            j = j + 1
        End If
        i = i + 1
    Wend
    
End If


If [Manual_CF_Flag] = 1 Or [Accelerated_Payment_Flag] = 1 Then
'the internal array limit is set to the entries in manual_cash_flow sheet if this is filled and used
intArray_Limit = [array_limit_mcf] - 1
'ElseIf dateFirst_Instalment_Date > 0 Then
'intArray_Limit = ((dateLast_Payment_Date - dateFirst_Instalment_Date) / 30) + intPayment_Frequency
Else
intArray_Limit = intCredit_Term
End If


i = 0
j = 0

Dim step_frequency As Integer

If [Manual_CF_Flag] = 1 Or [Accelerated_Payment_Flag] = 1 Then
    step_frequency = 1
Else
    step_frequency = intPayment_Frequency
End If

For i = 0 To intArray_Limit Step step_frequency


    '#0 Cash Flow Date
    If i = 0 Then
        If [Manual_CF_Flag] = 1 Then
        arrCash_Flow_Generation(j, 0) = arrMan_Cash_Flow(i, 1) 'for mcf first date is taken from manual payment schedule
        Else
        arrCash_Flow_Generation(j, 0) = datePayout_Date
        End If
    Else
        If [Manual_CF_Flag] = 1 Then
            'finish after last entry in mcf sheet
            If Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, 1) = "" Then
            GoTo Output:
            Else
                arrCash_Flow_Generation(j, 0) = arrMan_Cash_Flow(i, 1)
            End If
         Else
         dateCash_Flow_Date_Pre_Period = arrCash_Flow_Generation(j - 1, 0)
            If dateCash_Flow_Date_Pre_Period = dateLast_Payment_Date Then
            Exit For
            Else
         arrCash_Flow_Generation(j, 0) = fctCash_Flow_Date(intPayment_Frequency, _
                                                                 dateCash_Flow_Date_Pre_Period, _
                                                                 dateFirst_Instalment_Date, _
                                                                 arrSkip_Months())
            End If
        End If
    End If
    '#0 Cash Flow Date





    '#1 Period Counter excl Grace Period
    If i = 0 Then
        arrCash_Flow_Generation(j, 1) = 0
    Else
        dateCash_Flow_Date_Pre_Period = arrCash_Flow_Generation(j - 1, 0)
        dateCash_Flow_Date_Current_Period = arrCash_Flow_Generation(j, 0)
        intPeriod_Counter_excl_Grace_Period_Pre_Period = arrCash_Flow_Generation(j - 1, 1)
        
        arrCash_Flow_Generation(j, 1) = _
        fctPeriod_Counter_excl_Grace_Period(dateCash_Flow_Date_Pre_Period, _
                                            dateCash_Flow_Date_Current_Period, _
                                            dateFirst_Instalment_Date, _
                                            intPeriod_Counter_excl_Grace_Period_Pre_Period, _
                                            intPayment_Frequency)
    End If
    '#1 Period Counter excl Grace Period
    


    '#2 Period Counter incl Grace Period
    If i = 0 Then
        arrCash_Flow_Generation(j, 2) = 0
    Else
        intPeriod_Counter_excl_Grace_Period = arrCash_Flow_Generation(j, 1)
        
        arrCash_Flow_Generation(j, 2) = fctPeriod_Counter_incl_Grace_Period(intPeriod_Counter_excl_Grace_Period, _
                                                                            intInterest_Only_Period)
    End If
    '#2 Period Counter incl Grace Period



    '#3# Factor
    If i = 0 Then
    arrCash_Flow_Generation(j, 3) = 1
    Else
        'For principal only period the interest rate which is used to calculate the discount factor for the calculation of instalment
        'payments is set to 0 (dblzerointerest = 0). By this overall IRR of the contract will fall below nominal interest rate and reflect
        'periods where the total instalment is handled as principal payments (ie no interest payment = interest rate equals Zero). In the mdlMain
        'module the runoff is later corrected by calculating the interest payments based on the nomincal rate for all regular payment periods and
        'considering the total instalment as principal payments for the principal only periods.
        
        If [Manual_CF_Flag] = 1 And Worksheets("Manual_Cash_Flows").Cells(i + 2, 5) = "Principal Only" Then
        
        dateCash_Flow_Date_Pre_Period = arrCash_Flow_Generation(j - 1, 0)
        dateCash_Flow_Date_Current_Period = arrCash_Flow_Generation(j, 0)
        dblFactor_Pre_Period = arrCash_Flow_Generation(j - 1, 3)
                
        Dim dblzerointerest As Double
        dblzerointerest = 0
        arrCash_Flow_Generation(j, 3) = fctFactor(dblzerointerest, _
                                        dateCash_Flow_Date_Pre_Period, _
                                        dateCash_Flow_Date_Current_Period, _
                                        dblFactor_Pre_Period)
        Else
        
            
        dateCash_Flow_Date_Pre_Period = arrCash_Flow_Generation(j - 1, 0)
        dateCash_Flow_Date_Current_Period = arrCash_Flow_Generation(j, 0)
        dblFactor_Pre_Period = arrCash_Flow_Generation(j - 1, 3)
            
        arrCash_Flow_Generation(j, 3) = fctFactor(dblNOM_CR, _
                                        dateCash_Flow_Date_Pre_Period, _
                                        dateCash_Flow_Date_Current_Period, _
                                        dblFactor_Pre_Period)
        End If
    End If
    '#3# Factor
    

    
    
    '#4 Irregular_Instalment
    'Irregular cashflows comprise the initial financed amount, extraordinary payments during the life time of the contract
    'and the residual value payment at the end. In case the MCF function is used irregular cashflows are taken from the second
    'column of the MCF array (arrMan_Cash_Flow(i, 2)).
    If i = 0 Then
        If [Manual_CF_Flag] = 1 Or [Accelerated_Payment_Flag] = 1 Then
        arrCash_Flow_Generation(j, 4) = arrMan_Cash_Flow(i, 2)
        Else
        arrCash_Flow_Generation(j, 4) = -dblNAF
        End If
    Else
    If [Manual_CF_Flag] = 1 Or [Accelerated_Payment_Flag] = 1 Then
        arrCash_Flow_Generation(j, 4) = arrMan_Cash_Flow(i, 2)
    Else
        intPeriod_Counter_excl_Grace_Period = arrCash_Flow_Generation(j, 1)
        dateCash_Flow_Date = arrCash_Flow_Generation(j, 0)
        
        
        arrCash_Flow_Generation(j, 4) = fctIrregular_Instalment(strUS_OL, _
                                                                intPeriod_Counter_excl_Grace_Period, _
                                                                intCredit_Term, _
                                                                intPayment_Frequency, _
                                                                dblcontracted_RV, _
                                                                dateCash_Flow_Date, _
                                                                dateExtra_Ordinary_Payment_Date, _
                                                                dblExtra_Ordinary_Payment_Amount)
    End If
    End If
    '#4 Irregular_Instalment

    



    
    '#5 Irregular_Interest
    'Irregular interest comprises the interest during the interest only periods.
'    If i = 0 Then
'    arrCash_Flow_Generation(j, 5) = 0
'    Else
    
        If [Manual_CF_Flag] = 1 And counter > 0 And [Interest_Only_Periods] > 0 Then
        'If MCF function is used periods for periods where "interest only" was selected the amount of irregular interest is calculated based
        'on the NBV of the pre-period and the contract IRR. During "interest only periods" there is no principal payment and hence no amortization
        'of the contract.
        'Since interest only periods in the MCF sheet can not only occur at the beginning of the contract but also in between the NBV at the beginning
        'of an interest only periods needs to be known in order to be able to calculate the correct amount of interest. Therefore, at the end of this module
        'there is a check whether there are any interest only periods and if yes, the initial cashflow from the MCF sheet is used to calculated
        'a complete runoff and the module mdlCash_Flow_Generation is started again. For more details see "Iteration process for
        'Only Interest Periods" at the end of this module.

            If Worksheets("Manual_Cash_Flows").Cells(i + 2, 5) = "Interest Only" Then
            'The irregular interest are calculated using the NBV from the pre-period which was calculated in the "Iteration process for
            'Only Interest Periods" procedure at the end of this module and the IRR.
                arrCash_Flow_Generation(j, 5) = lContract.LiqRunoff(i).NBV * lContract.IRR * fct_DiffDays30(Worksheets("Manual_Cash_Flows").Cells(i + 1, 2), Worksheets("Manual_Cash_Flows").Cells(i + 2, 2)) / 360
            End If
        Else
        'For contracts with "interest only periods" only at the beginning of the contract there is the option to use the "Interest Only Period" function in the Optional
        'Information Section of the Input Mask. In this case the fctIrregular_Interest is used to calculate irregular interest. For more details please see the documentation
        'of the fctIrregular_Interest.
        
        'Update the function so that interest only period also works with payment mode in advance
            intPeriod_Counter_excl_Grace_Period = arrCash_Flow_Generation(j, 1)
            dateCash_Flow_Date_Current_Period = arrCash_Flow_Generation(j, 0)
            
            If strPayment_Mode = "In Arrears" And j > 0 Then
                dateCash_Flow_Date_Pre_Period = arrCash_Flow_Generation(j - 1, 0)
            Else
                dateCash_Flow_Date_Next_Period = arrCash_Flow_Generation(j + 1, 0)
            End If
            
            If i = 0 And strPayment_Mode = "In Arrears" Then
                arrCash_Flow_Generation(j, 5) = 0
            Else
                arrCash_Flow_Generation(j, 5) = fctIrregular_Interest(intPeriod_Counter_excl_Grace_Period, _
                                                                  intInterest_Only_Period, _
                                                                  dblNAF, _
                                                                  dblNOM_CR, _
                                                                  dateCash_Flow_Date_Pre_Period, _
                                                                  dateCash_Flow_Date_Current_Period, _
                                                                  dateCash_Flow_Date_Next_Period, _
                                                                  strPayment_Mode)
            End If
        End If
'    End If
    
       
    '#5 Irregular_Interest


    '#6 Regular_Payment_Excl_Payment_Mode
    If i = 0 Then
        arrCash_Flow_Generation(j, 6) = 0
    Else
        dateCash_Flow_Date = arrCash_Flow_Generation(j, 0)
        intPeriod_Counter_incl_Grace_Period = arrCash_Flow_Generation(j, 2)
        
        arrCash_Flow_Generation(j, 6) = fctRegular_Payment_Excl_Payment_Mode(strPayment_Mode, _
                                                                             dateCash_Flow_Date, _
                                                                             dateFirst_Instalment_Date, _
                                                                             intPeriod_Counter_excl_Grace_Period, _
                                                                             intCredit_Term, _
                                                                             intPayment_Frequency, _
                                                                             intInterest_Only_Period, _
                                                                             strUS_OL, _
                                                                             dblLast_Instalment)
    End If
    '#6 Regular_Payment_Excl_Payment_Mode
    
    j = j + 1

Next i

If [Manual_CF_Flag] <> 1 And [Accelerated_Payment_Flag] <> 1 Then
intArray_Limit = (j * intPayment_Frequency) - intPayment_Frequency
End If

Output:
j = 0
For i = 0 To intArray_Limit Step step_frequency '+ intPayment_Frequency Step intPayment_Frequency
    
    '#7 Regular_Payment_Incl_Payment_Mode
'needed inputs for #7#
'--------------------------------------------------------------------------------
'a) strPayment_Mode
'--------------------------------------------------------------------------------
'b) dateCash_Flow_Date_Next_Period
        dateCash_Flow_Date_Pre_Period = arrCash_Flow_Generation(j, 0)
        dateCash_Flow_Date_Next_Period = fctCash_Flow_Date(intPayment_Frequency, _
                                                                 dateCash_Flow_Date_Pre_Period, _
                                                                 dateFirst_Instalment_Date, _
                                                                 arrSkip_Months())
'--------------------------------------------------------------------------------
'c) dateFirst_Instalment_Date
'--------------------------------------------------------------------------------
'd) intRegular_Payment_Excl_Payment_Mode_Current_Period
    intRegular_Payment_Excl_Payment_Mode_Current_Period = arrCash_Flow_Generation(j, 6)
'--------------------------------------------------------------------------------
'e) intRegular_Payment_Excl_Payment_Mode_Next_Period
    'Inputs for e)
    dateCash_Flow_Date = dateCash_Flow_Date_Next_Period
    'intPeriod_Counter_excl_Grace_Period
        
        dateCash_Flow_Date_Current_Period = dateCash_Flow_Date_Next_Period
        intPeriod_Counter_excl_Grace_Period_Pre_Period = arrCash_Flow_Generation(j, 1)

        intPeriod_Counter_excl_Grace_Period = _
        fctPeriod_Counter_excl_Grace_Period(dateCash_Flow_Date_Pre_Period, _
                                            dateCash_Flow_Date_Current_Period, _
                                            dateFirst_Instalment_Date, _
                                            intPeriod_Counter_excl_Grace_Period_Pre_Period, _
                                            intPayment_Frequency)
                                            
    intRegular_Payment_Excl_Payment_Mode_Next_Period = fctRegular_Payment_Excl_Payment_Mode(strPayment_Mode, _
                                                                         dateCash_Flow_Date, _
                                                                         dateFirst_Instalment_Date, _
                                                                         intPeriod_Counter_excl_Grace_Period, _
                                                                         intCredit_Term, _
                                                                         intPayment_Frequency, _
                                                                         intInterest_Only_Period, _
                                                                         strUS_OL, _
                                                                         dblLast_Instalment)

'--------------------------------------------------------------------------------
    
    arrCash_Flow_Generation(j, 7) = _
    fctRegular_Payment_Incl_Payment_Mode(strPayment_Mode, _
                                         dateCash_Flow_Date_Next_Period, _
                                         datePayout_Date, _
                                         intPayment_Frequency, _
                                         dateFirst_Instalment_Date_Input, _
                                         dateFirst_Instalment_Date, _
                                         intRegular_Payment_Excl_Payment_Mode_Current_Period, _
                                         intRegular_Payment_Excl_Payment_Mode_Next_Period, _
                                         i)
    '#7 Regular_Payment_Incl_Payment_Mode

    
    '#8 Regular
    intRegular_Payment_Incl_Payment_Mode = arrCash_Flow_Generation(j, 7)
    If [Manual_CF_Flag] = 1 Or [Accelerated_Payment_Flag] = 1 Then
            If Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, 4) = "" Then
            GoTo Output2:
            Else
                arrCash_Flow_Generation(j, 8) = arrMan_Cash_Flow(i, 4)
            End If
    Else
        arrCash_Flow_Generation(j, 8) = fctRegular(intRegular_Payment_Incl_Payment_Mode)
    End If
    '#8 Regular
    
    j = j + 1
Next i
Output2:

'For i = 0 To j - 1
'    Debug.Print arrCash_Flow_Generation(i, 0) & "; " & arrCash_Flow_Generation(i, 1) & "; " & _
'    arrCash_Flow_Generation(i, 2) & "; " & arrCash_Flow_Generation(i, 3) & "; " & _
'    arrCash_Flow_Generation(i, 4) & "; " & arrCash_Flow_Generation(i, 5) & _
'    "; " & arrCash_Flow_Generation(i, 6) & "; " & arrCash_Flow_Generation(i, 7) & _
'    "; " & arrCash_Flow_Generation(i, 8) & "; " & arrCash_Flow_Generation(i, 9) & _
'    "; " & arrCash_Flow_Generation(i, 10) & "; " & arrCash_Flow_Generation(i, 11) & _
'    "; " & arrCash_Flow_Generation(i, 12) & "; " & arrCash_Flow_Generation(i, 13) & _
'    "; " & arrCash_Flow_Generation(i, 14) & "; " & arrCash_Flow_Generation(i, 15) & _
'    "; " & arrCash_Flow_Generation(i, 16) & "; " & arrCash_Flow_Generation(i, 17) & _
'    "; " & arrCash_Flow_Generation(i, 18) & "; " & arrCash_Flow_Generation(i, 19) & _
'    "; " & arrCash_Flow_Generation(i, 20)
'Next i



'--------------------------------------------------------------------------
j = 0
For i = 0 To intArray_Limit Step step_frequency
   
   dblZaehler = dblZaehler + arrCash_Flow_Generation(j, 3) * arrCash_Flow_Generation(j, 4) _
                + arrCash_Flow_Generation(j, 3) * arrCash_Flow_Generation(j, 5)

   dblNenner = dblNenner + arrCash_Flow_Generation(j, 3) * arrCash_Flow_Generation(j, 8)
   dblZaehler_variable = dblZaehler_variable + arrCash_Flow_Generation(j, 4)
   dblNenner_variable = dblNenner_variable + arrCash_Flow_Generation(j, 8)
   dblSum_Irregular_Instalment = dblSum_Irregular_Instalment + arrCash_Flow_Generation(j, 4)
j = j + 1
Next i


dblSum = dblSum_Irregular_Instalment
dblRegular_Payment = dblZaehler / -dblNenner
dblRegular_Payment_variable = dblZaehler_variable / -dblNenner_variable


If strRORACTargetCase = "No" And strLast = "" Then
    If [Interest_Type] <> "Fix" And [amortization_method] = 1 Then
        Worksheets("Index").Range("mdl_Installment").value = dblRegular_Payment_variable
        dblAct_Installment = dblRegular_Payment_variable
    Else
        Worksheets("Index").Range("mdl_Installment").value = dblRegular_Payment
        dblAct_Installment = dblRegular_Payment
    End If
End If

'--------------------------------------------------------------------------
    
j = 0
For i = 0 To intArray_Limit Step step_frequency
    
    '#9 Regular_Payments
    intRegular = arrCash_Flow_Generation(j, 8)
     
    arrCash_Flow_Generation(j, 9) = fctRegular_Payments(intRegular, dblRegular_Payment)
    '#9 Regular_Payments
    
    
    '#10 Total_Cash_Flow_excl_IDC_Subs
    dblIrregular_Instalment = arrCash_Flow_Generation(j, 4)
    dblIrregular_Interest = arrCash_Flow_Generation(j, 5)
    dblRegular_Payments = arrCash_Flow_Generation(j, 9)
    
    arrCash_Flow_Generation(j, 10) = fctTotal_Cash_Flow_excl_IDC_Subs(dblIrregular_Instalment, _
                                                                      dblIrregular_Interest, _
                                                                      dblRegular_Payments)
    '#10 Total_Cash_Flow_excl_IDC_Subs
             
             
    '#11 Total_Cash_Flow_incl_IDC_Subs
    dblTotal_Cash_Flow_excl_IDC_Subs = arrCash_Flow_Generation(j, 10)
    If i = 0 Then
        arrCash_Flow_Generation(j, 11) = dblTotal_Cash_Flow_excl_IDC_Subs - dblInititial_Direct_Cost + dblSubsidies
    Else
        arrCash_Flow_Generation(j, 11) = fctTotal_Cash_Flow_incl_IDC_Subs(dblTotal_Cash_Flow_excl_IDC_Subs)
    End If
    '#11 Total_Cash_Flow_incl_IDC_Subs
    
    
    '#12 Regular_Principal
    intRegular = arrCash_Flow_Generation(j, 8)
     
    arrCash_Flow_Generation(j, 12) = fctRegular_Payments_variable(intRegular, dblRegular_Payment_variable)
    '#12 Regular_Principal

j = j + 1
Next i
12


'--------------------------------------------------------------------------
'calculation of effective maturity for BASEL II formula
j = 1
dblZaehler = 0
dblNenner = 0
For i = 1 To intArray_Limit Step step_frequency

If i = intArray_Limit Then
dblZaehler = dblZaehler + (arrCash_Flow_Generation(j, 1) * (arrCash_Flow_Generation(j, 11) - [Add_Coll]))
Else
dblZaehler = dblZaehler + arrCash_Flow_Generation(j, 1) * arrCash_Flow_Generation(j, 11)
End If


If i = intArray_Limit Then
dblNenner = dblNenner + arrCash_Flow_Generation(j, 11) - [Add_Coll]
Else
dblNenner = dblNenner + arrCash_Flow_Generation(j, 11)
End If

j = j + 1
Next i

If dblZaehler / dblNenner / 12 < 5 Then
    If dblZaehler / dblNenner / 12 > 1 Then
        dblEffective_Maturity = dblZaehler / dblNenner / 12
    Else
        dblEffective_Maturity = 1
    End If
Else
    dblEffective_Maturity = 5
End If

'--------------------------------------------------------------------------




j = 0
For i = 0 To intArray_Limit Step step_frequency
    
    '#14 Total_Principal_excl_Interest
    arrCash_Flow_Generation(j, 14) = arrCash_Flow_Generation(j, 4) + arrCash_Flow_Generation(j, 12)
    '#14 Total_Principal_excl_Interest
    

    '#16 Principal_Interest_Binding
    If i = 0 Then
        arrCash_Flow_Generation(j, 16) = 0
    Else
        intPeriod_Counter_excl_Grace_Period = arrCash_Flow_Generation(j, 1)
        dblTotal_Principal_excl_Interest = arrCash_Flow_Generation(j, 14)
        dblOutstanding_Liquidity_Binding_Pre_Period = arrCash_Flow_Generation(j - 1, 17)
    
        arrCash_Flow_Generation(j, 16) = _
                                        fctPrincipal_Interest_Binding(intPeriod_Counter_excl_Grace_Period, _
                                                                      intInterest_Type_num, _
                                                                      dblTotal_Principal_excl_Interest, _
                                                                      dblOutstanding_Liquidity_Binding_Pre_Period, _
                                                                      intPayment_Frequency)
    End If
    '#16 Principal_Interest_Binding
    

    '#17 Outstanding_Interest_Binding
    If i = 0 Then
        arrCash_Flow_Generation(j, 17) = -arrCash_Flow_Generation(j, 14)
    Else
        dblOutstanding_Interest_Binding_Pre_Period = arrCash_Flow_Generation(j - 1, 17)
        dblPrincipal_Interest_Binding = arrCash_Flow_Generation(j, 16)
    
        arrCash_Flow_Generation(j, 17) = fctOutstanding_Interest_Binding(dblOutstanding_Interest_Binding_Pre_Period, _
                                                                         dblPrincipal_Interest_Binding)
    End If
    '#17 Outstanding_Interest_Binding
    
 
 
    '#18 Cash_Flow_Date
    arrCash_Flow_Generation(j, 18) = arrCash_Flow_Generation(j, 0)
    '#18 Cash_Flow_Date

    
    '#19 Total_Fix_Cash_Flows
    If i = 0 Then
        dblTotal_Cash_Flow_incl_IDC_Subs = arrCash_Flow_Generation(j, 11)
        dblTotal_Principal_excl_Interest = arrCash_Flow_Generation(j, 14)
        dblAmmortization_Method = [amortization_method]
        
        arrCash_Flow_Generation(j, 19) = fctTotal_Fix_Cash_Flows(strInterest_Type, _
                                                                 dblTotal_Cash_Flow_incl_IDC_Subs, _
                                                                 dblTotal_Principal_excl_Interest, _
                                                                 dblInititial_Direct_Cost, _
                                                                 dblSubsidies, _
                                                                 i, _
                                                                 dblAmmortization_Method)
    Else
        dblTotal_Cash_Flow_incl_IDC_Subs = arrCash_Flow_Generation(j, 11)
        dblTotal_Principal_excl_Interest = arrCash_Flow_Generation(j, 14)
        
        arrCash_Flow_Generation(j, 19) = fctTotal_Fix_Cash_Flows(strInterest_Type, _
                                                                 dblTotal_Cash_Flow_incl_IDC_Subs, _
                                                                 dblTotal_Principal_excl_Interest, _
                                                                 dblInititial_Direct_Cost, _
                                                                 dblSubsidies, _
                                                                 i, _
                                                                 dblAmmortization_Method)
    End If
    '#19 Total_Fix_Cash_Flows
    


    '#20 Total_Variable_Cash_Flows
    If i = 0 Then
       arrCash_Flow_Generation(j, 20) = 0
    Else
        dblOutstanding_Interest_Binding_Pre_Period = arrCash_Flow_Generation(j - 1, 17)
        dateCash_Flow_Date_Pre_Period = arrCash_Flow_Generation(j - 1, 0)
        dateCash_Flow_Date_Current_Period = arrCash_Flow_Generation(j, 0)
        strFlag = "no"
        
        
        arrCash_Flow_Generation(j, 20) = fctTotal_Variable_Cash_Flows(strInterest_Type, _
                                                                      dblOutstanding_Interest_Binding_Pre_Period, _
                                                                      dblNOM_CR, _
                                                                      dateCash_Flow_Date_Pre_Period, _
                                                                      dateCash_Flow_Date_Current_Period, _
                                                                      strFlag, _
                                                                      dblAmmortization_Method)
    End If
    '#20 Total_Variable_Cash_Flows
    
    
j = j + 1
Next i



'For i = 0 To intArray_Limit
'    Debug.Print arrCash_Flow_Generation(i, 1); arrCash_Flow_Generation(i, 18); arrCash_Flow_Generation(i, 19); arrCash_Flow_Generation(i, 20)
'Next i
'Debug.Print "-------------------------------------------------------------------"

'For i = 0 To j - 1
'    Debug.Print "(0)" & arrCash_Flow_Generation(i, 0) & "; " & "(1)" & arrCash_Flow_Generation(i, 1) & "; " & _
'    "(2)" & arrCash_Flow_Generation(i, 2) & "; " & "(3)" & arrCash_Flow_Generation(i, 3) & "; " & _
'    "(4)" & arrCash_Flow_Generation(i, 4) & "; " & "(5)" & arrCash_Flow_Generation(i, 5) & _
'    "; " & "(6)" & arrCash_Flow_Generation(i, 6) & "; " & "(7)" & arrCash_Flow_Generation(i, 7) & _
'    "; " & "(8)" & arrCash_Flow_Generation(i, 8) & "; " & "(9)" & arrCash_Flow_Generation(i, 9) & _
'    "; " & "(10)" & arrCash_Flow_Generation(i, 10) & "; " & "(11)" & arrCash_Flow_Generation(i, 11) & _
'        "; " & "(12)" & arrCash_Flow_Generation(i, 12) & "; " & "(13)" & arrCash_Flow_Generation(i, 13) & _
'    "; " & "(14)" & arrCash_Flow_Generation(i, 14) & "; " & "(15)" & arrCash_Flow_Generation(i, 15) & _
'    "; " & "(16)" & arrCash_Flow_Generation(i, 16) & "; " & "(17)" & arrCash_Flow_Generation(i, 17) & _
'        "; " & "(18)" & arrCash_Flow_Generation(i, 18) & "; " & "(19)" & arrCash_Flow_Generation(i, 19) & _
'    "; " & "(20)"
'
'Next i

'
'For i = 0 To j - 1
'    Debug.Print arrCash_Flow_Generation(i, 0) & ";" & arrCash_Flow_Generation(i, 1) & ";" & _
'     arrCash_Flow_Generation(i, 2) & ";" & arrCash_Flow_Generation(i, 3) & ";" & _
'     arrCash_Flow_Generation(i, 4) & ";" & arrCash_Flow_Generation(i, 5) & _
'    ";" & arrCash_Flow_Generation(i, 6) & ";" & arrCash_Flow_Generation(i, 7) & _
'    ";" & arrCash_Flow_Generation(i, 8) & ";" & arrCash_Flow_Generation(i, 9) & _
'    ";" & arrCash_Flow_Generation(i, 10) & ";" & arrCash_Flow_Generation(i, 11) & _
'        ";" & arrCash_Flow_Generation(i, 12) & ";" & arrCash_Flow_Generation(i, 13) & _
'    ";" & arrCash_Flow_Generation(i, 14) & ";" & arrCash_Flow_Generation(i, 15) & _
'    ";" & arrCash_Flow_Generation(i, 16) & ";" & arrCash_Flow_Generation(i, 17) & _
'        ";" & arrCash_Flow_Generation(i, 18) & ";" & arrCash_Flow_Generation(i, 19) & _
'    ";" & arrCash_Flow_Generation(i, 20)
'
'Next i

'
'Debug.Print "-------------------------------------------------------------------"

ReDim arrcash_flow_results(0 To j - 1, 0 To 3)
For i = 0 To j - 1
    arrcash_flow_results(i, 0) = arrCash_Flow_Generation(i, 1)
    arrcash_flow_results(i, 1) = arrCash_Flow_Generation(i, 18)
    arrcash_flow_results(i, 2) = arrCash_Flow_Generation(i, 19)
    arrcash_flow_results(i, 3) = arrCash_Flow_Generation(i, 20)
    'Debug.Print arrcash_flow_results(i, 0) & ";" & arrcash_flow_results(i, 1) & ";" & arrcash_flow_results(i, 2) & ";" & arrcash_flow_results(i, 3)
Next i

fctCash_Flow_Generation = arrcash_flow_results()

'Target RoRAC Case 4
If [Target_RORAC_Case] = "Yes" Then
    Select Case [Target_Type]
    Case 4
    
        If [Interest_Type] <> "Fix" And [amortization_method] = 1 Then
            dblAct_Installment = dblRegular_Payment_variable + arrCash_Flow_Generation(1, 20)
        Else
            dblAct_Installment = dblRegular_Payment
        End If

        ReDim arrTarget_CF(0 To j - 1, 0 To 1)
        For i = 0 To j - 1
            arrTarget_CF(i, 0) = arrCash_Flow_Generation(i, 3)
            arrTarget_CF(i, 1) = arrCash_Flow_Generation(i, 4) + arrCash_Flow_Generation(i, 5) + dblTarget_Installment * arrCash_Flow_Generation(i, 8)
            dblNPV = dblNPV + arrTarget_CF(i, 0) * arrTarget_CF(i, 1)
            dblIni_NPV = dblIni_NPV + arrTarget_CF(i, 1)
        Next i
    End Select
End If
'Target RoRAC Case 4

'Iteration process for Only Interest Periods

If [Manual_CF_Flag] = 1 And counter - 1 < intArray_Limit And [Interest_Only_Periods] > 0 Then
    Call sub_CalcCF(lContract, arrcash_flow_results(), laCurve, laSpreads, dblCalculation_Date, _
    strMM, strSW, intCompounding_Frequency, intAnnualized)
    GoTo Iteration
End If

'Iteration process for Accelerated Payments
'Updated so that accelerated payment also works with payment mode in advance

If [Accelerated_Payment_Flag] = 1 Then
    Dim k As Integer
    For k = 0 To intArray_Limit
        If arrCash_Flow_Generation(k, 0) = [Accelerated_Payment_End] Then
            'because Accelerated End Payent relates to customer runoff wo subs and idc
            If [IDC] - dblSubsidies <> 0 Then
                arrcash_flow_results(0, 2) = arrcash_flow_results(0, 2) + [IDC] - dblSubsidies
            Else
                arrcash_flow_results(0, 2) = arrcash_flow_results(0, 2)
            End If
            
            Call sub_CalcCF(lContract, arrcash_flow_results(), laCurve, laSpreads, dblCalculation_Date, _
            strMM, strSW, intCompounding_Frequency, intAnnualized)
            
            If strPayment_Mode = "In Arrears" Then
                If Abs(lContract.LiqRunoff(k + 1).NBV - [end_value_acc_payment]) > 0.004 Then
                    dblAccPayment = dblAccPayment + ((lContract.LiqRunoff(k + 1).NBV - [end_value_acc_payment]) / [Periods_Acc_Payments])
                    GoTo Iteration
                Else
                    Set wksIndex = Sheets("Index")
                    wksIndex.Range("Accelerated_Payment").ClearContents
                    wksIndex.Range("Accelerated_Payment") = dblAccPayment
                End If
            Else
                If Abs(lContract.LiqRunoff(k + 1).NBV + lContract.LiqRunoff(k + 1).NBV * dblNOM_CR * (fct_DiffDays30(arrcash_flow_results(k, 1), arrcash_flow_results(k + 1, 1)) / 360) - [end_value_acc_payment]) > 0.004 Then
                    dblAccPayment = dblAccPayment + ((lContract.LiqRunoff(k + 1).NBV + lContract.LiqRunoff(k + 1).NBV * dblNOM_CR * (fct_DiffDays30(arrcash_flow_results(k, 1), arrcash_flow_results(k + 1, 1)) / 360) - [end_value_acc_payment]) / [Periods_Acc_Payments])
                    GoTo Iteration
                Else
                    Set wksIndex = Sheets("Index")
                    wksIndex.Range("Accelerated_Payment").ClearContents
                    wksIndex.Range("Accelerated_Payment") = dblAccPayment
                End If
            End If
        Exit For
        End If
    Next k
End If
    

' Deposit calculation Mexico and Israel

If ([Country_Short] = "MEX" Or [Country_Short] = "ISR") And [DepositAmtType] = 1 Then
    If [TaxRefBase] = "Payment" Then
    Worksheets("New Input Mask").Range("H43").value = dblRegular_Payment * [NumberPmtDeposit] * (1 + [tax_rate])
    Else
    Worksheets("New Input Mask").Range("H43").value = dblRegular_Payment * [NumberPmtDeposit]
    End If
Application.Calculate
End If

If [Country_Short] = "ISR" And [DepositAmtType] = 2 Then
    Worksheets("New Input Mask").Range("H43").value = Worksheets("Local_Sheet").Range("R30").value
    Application.Calculate
End If


'For i = 1 To j - 1
'Debug.Print arrCash_Flow_Results(0, 0)
'    arrCash_Flow_Results(i, 0) = arrCash_Flow_Generation(i, 0)
'    arrCash_Flow_Results(i, 1) = arrCash_Flow_Generation(i, 1)
'    arrCash_Flow_Results(i, 2) = arrCash_Flow_Generation(i, 2)
'    arrCash_Flow_Results(i, 3) = arrCash_Flow_Generation(i, 3)
'    arrCash_Flow_Results(i, 4) = arrCash_Flow_Generation(i, 4)
'    arrCash_Flow_Results(i, 5) = arrCash_Flow_Generation(i, 5)
'    arrCash_Flow_Results(i, 6) = arrCash_Flow_Generation(i, 6)
'    arrCash_Flow_Results(i, 7) = arrCash_Flow_Generation(i, 7)
'    arrCash_Flow_Results(i, 8) = arrCash_Flow_Generation(i, 8)
'    arrCash_Flow_Results(i, 9) = arrCash_Flow_Generation(i, 9)
'    arrCash_Flow_Results(i, 10) = arrCash_Flow_Generation(i, 10)
'    arrCash_Flow_Results(i, 11) = arrCash_Flow_Generation(i, 11)
'    arrCash_Flow_Results(i, 12) = arrCash_Flow_Generation(i, 12)
'    arrCash_Flow_Results(i, 13) = arrCash_Flow_Generation(i, 13)
'    arrCash_Flow_Results(i, 14) = arrCash_Flow_Generation(i, 14)
'    arrCash_Flow_Results(i, 15) = arrCash_Flow_Generation(i, 15)
'    arrCash_Flow_Results(i, 16) = arrCash_Flow_Generation(i, 16)
'    arrCash_Flow_Results(i, 17) = arrCash_Flow_Generation(i, 17)
'    arrCash_Flow_Results(i, 18) = arrCash_Flow_Generation(i, 18)
'    arrCash_Flow_Results(i, 19) = arrCash_Flow_Generation(i, 19)
'    arrCash_Flow_Results(i, 20) = arrCash_Flow_Generation(i, 20)
'Next i

'cash flows for manual cash flow sheet
'If [Manual_CF_Flag] = 1 Then 'strRORACTargetCase = "No" And strLast = "" And
'For i = 0 To intCredit_Term 'Step intPayment_Frequency
'    For j = 1 To 3
'        If j <> 3 Then
'        Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, j) = arrCash_Flow_Generation(j, 4)
'        Else
'        Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, j + 1) = arrCash_Flow_Generation(j, 8)
'        End If
'     Next j
'Next i
'End If


End Function




--- Macro File: mdlFunctions.bas ---
Attribute VB_Name = "mdlFunctions"
Option Explicit
Public Function fctClear_Range(rngRange As Range)

Dim i As Integer
Dim j As Integer

For i = 1 To rngRange.Rows.Count
    For j = 1 To rngRange.Columns.Count
        rngRange(i, j) = ""
    Next j
Next i
End Function






--- Macro File: Frm_Target_Sub.frm ---
Attribute VB_Name = "Frm_Target_Sub"
Attribute VB_Base = "0{EDD6F7CB-B885-47C0-8E16-E5CB575B0D12}{3525BC9C-F5D8-409B-82F9-07E9A82055F2}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private Sub UserForm_activate()
Label2.Caption = "Required Subsidies"
Label4.Caption = Left([Deal_Currency], 3)
End Sub

Private Sub CommandButton1_Click()

Dim value As Double


If (TextBox1.value <> "" And Not IsNumeric(TextBox1.value)) Or (TextBox1.value = "") Then
        MsgBox "Please enter a correct Target RORAC"
        Exit Sub
End If

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

If Application.UseSystemSeparators = True Then
    value = CDbl(TextBox1.value)
Else
    value = CDbl(Replace(TextBox1.value, strApp_Dec_Separator, strOS_Dec_Separator))
End If

[Target_RORAC] = value / 100
[Target_RORAC_Case] = "Yes"
[Target_Type] = "3"
Call unprotectInput

Call prcStartCalculation

If Application.UseSystemSeparators = True Then
    TextBox2.value = WorksheetFunction.Round([Target_Rate], 2)
Else
    TextBox2.value = Replace(WorksheetFunction.Round([Target_Rate], 2), strOS_Dec_Separator, strApp_Dec_Separator)
End If

Label5.Caption = [Target_Rate]
[Target_RORAC_Case] = "No"

Call protectInput

End Sub

Private Sub CommandButton2_Click()

If (TextBox2.value = "") Then
        MsgBox "Please Calculate a Target Customer Rate"
        Exit Sub
End If


Worksheets("New Input Mask").Range("H39").value = CDbl(Label5.Caption)
'[Nom_CR_MCF] = CDbl(Label5.Caption)
'Worksheets("New Input Mask").ComboBox3.Value = "Customer Rate"

Unload Me

Call unprotectInput

Call prcStartCalculation

[Target_RORAC_Case] = "No"

Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "Target RORAC was successfully calculated"
End Sub

Private Sub CommandButton3_Click()
[Target_RORAC_Case] = "No"
Unload Me
End Sub



--- Macro File: Library_Cashflow.bas ---
Attribute VB_Name = "Library_Cashflow"
Option Explicit

'// calculation of IRR, MFR and MFS based on cashflow profile

Global Const cMaxCashflow = 400

Public Type typRunoff
    Date As Double
    NBV As Double
    DCF As Double
End Type

Public Type typContract
    IRR As Double
    MFR As Double
    MFS As Double
    LiqRunoff() As typRunoff
End Type

Type typSingleCF
    Date As Date
    Diff As Double
    CashFlow As Double
    NBVnom As Double
    NBVeff As Double
    DCF As Double
    Flag As Byte
End Type

Type typCashflow
    Count As Integer
    Count2 As Integer
    IReff As Double
    IRnom As Double
    a(1 To cMaxCashflow) As typSingleCF
    Eps As Double
    PVCap As Double
End Type

Public Function fct_MarginNom(CF As typCashflow, Optional pdblPVAdd As Double = 0) As Double

    Dim lintRun As Integer
    Dim ldblPV As Double
    Dim ldblCap As Double
    
    fct_MarginNom = 0
    
    If CF.Count2 < 1 Then Exit Function
    
    ldblPV = CF.a(1).CashFlow * CF.a(1).DCF
    ldblCap = 0
    For lintRun = 2 To CF.Count2
        With CF.a(lintRun)
            ldblPV = ldblPV + .CashFlow * .DCF
            ldblCap = ldblCap + CF.a(lintRun - 1).NBVnom * .DCF * .Diff
        End With
    Next lintRun
    
    CF.PVCap = ldblCap
    
    fct_MarginNom = (ldblPV + pdblPVAdd) / ldblCap

End Function

Public Function fct_MarginEff(CF As typCashflow, Optional pdblStart As Double = 0, Optional pdblFS As Double = 0) As Double

    Dim lintRun As Integer
    Dim ldblDer As Double
    Dim ldblCap As Double
    Dim ldblMargin As Double
    Dim lintSolution As Integer
    Dim lintTry As Integer
    Dim ldblFactor As Double
    
    fct_MarginEff = 0
    
    If CF.Count2 < 1 Then Exit Function
    
    ldblMargin = pdblStart
    lintSolution = False
    lintTry = 0
    
    Do While Not lintSolution And lintTry < 40
        
        ldblCap = -CF.a(1).CashFlow * CF.a(1).DCF
        ldblDer = 0
        For lintRun = 2 To CF.Count2
            With CF.a(lintRun)
                ldblFactor = (1 + ldblMargin) ^ .Diff
                ldblCap = ldblCap + (CF.a(lintRun - 1).NBVeff * (ldblFactor + (1 + pdblFS) ^ .Diff - 2) - .CashFlow) * .DCF
                ldblDer = ldblDer + CF.a(lintRun - 1).NBVeff * ldblFactor / (1 + ldblMargin) * .Diff * .DCF
            End With
        Next lintRun
    
        If Abs(ldblCap) < CF.Eps Then
            lintSolution = True
            fct_MarginEff = ldblMargin
        Else
            lintTry = lintTry + 1
            ldblMargin = ldblMargin - ldblCap / ldblDer
            If ldblMargin < -1 Then ldblMargin = 0
        End If
    
    Loop

End Function
Public Function fct_MFREff(CF As typCashflow, pdblIREff As Double, Optional pdblStart As Double = 0, Optional pdblPVAdd As Double = 0) As Double

    Dim lintRun As Integer
    Dim ldblDer As Double
    Dim ldblCap As Double
    Dim ldblMFR As Double
    Dim lintSolution As Integer
    Dim lintTry As Integer
    Dim ldblFactor As Double
    
    fct_MFREff = 0
    If CF.Count2 < 1 Then Exit Function
    
    ldblMFR = pdblStart
    lintSolution = False
    lintTry = 0
    
    Do While Not lintSolution And lintTry < 40
        
        ldblCap = -CF.a(1).CashFlow * CF.a(1).DCF
        ldblDer = 0
        For lintRun = 2 To CF.Count2
            With CF.a(lintRun)
                ldblFactor = (1 + pdblIREff) ^ .Diff - (1 + ldblMFR) ^ .Diff
                ldblCap = ldblCap + (CF.a(lintRun - 1).NBVeff * ldblFactor - .CashFlow) * .DCF
                ldblDer = ldblDer - CF.a(lintRun - 1).NBVeff * (1 + ldblMFR) ^ (.Diff - 1) * .Diff * .DCF
            End With
        Next lintRun
    
        If Abs(ldblCap - pdblPVAdd) < CF.Eps Then
            lintSolution = True
            fct_MFREff = ldblMFR
        Else
            lintTry = lintTry + 1
            ldblMFR = ldblMFR - (ldblCap - pdblPVAdd) / ldblDer
        End If
    
    Loop

End Function


Public Sub sub_CalcCF(pContract As typContract, paCF() As Variant, _
                           paYields() As typCurveInput, paSpreads() As typCurveInput, pdblCalcDate As Double, _
                           pstrMM As String, pstrSW As String, pintComp As Integer, pintAnnualized As Integer)


    Dim lCFProfile As typCashflow
    Dim lintRun As Integer
    Dim lintVar As Integer
    
    '// read cashflow profile
    lintVar = False
    lCFProfile.Count = 0
    For lintRun = LBound(paCF) To UBound(paCF)
        If IsDate(paCF(lintRun, 1)) Then
            lCFProfile.Count = lCFProfile.Count + 1
            lCFProfile.a(lCFProfile.Count).Date = paCF(lintRun, 1)
            lCFProfile.a(lCFProfile.Count).CashFlow = paCF(lintRun, 2)
            lintVar = lintVar Or (paCF(lintRun, 3) <> 0)
            lCFProfile.a(lCFProfile.Count).Flag = 1
        End If
    Next lintRun
        
    '// calculation of IRR and LiqRunoff
    If [Country_Short] = "KOR" Then
        lCFProfile.Eps = 0.0004
    Else
        lCFProfile.Eps = 0.0001
    End If
    lCFProfile.Count2 = lCFProfile.Count
    If fct_IRR(lCFProfile, "30/360") Then
        pContract.IRR = lCFProfile.IRnom
        ReDim pContract.LiqRunoff(1 To lCFProfile.Count)
        
'        For lintRun = 1 To lCFProfile.Count
'        Debug.Print lCFProfile.a(lintRun).Date, lCFProfile.a(lintRun).NBVnom
'        Next lintRun
        
        For lintRun = 1 To lCFProfile.Count
            pContract.LiqRunoff(lintRun).Date = lCFProfile.a(lintRun).Date
            pContract.LiqRunoff(lintRun).NBV = lCFProfile.a(lintRun).NBVnom
        Next lintRun
    Else
        Exit Sub
    End If
    
    '// init cashflow with DCF (spreads)
    Call sub_CalcDCF(paSpreads, pintComp, pstrSW, pstrMM, gDCF, pdblCalcDate, pintAnnualized)
    For lintRun = 1 To lCFProfile.Count2
        With lCFProfile.a(lintRun)
            .DCF = fct_GetDCF(CDbl(.Date), gDCF())
        End With
    Next lintRun

    '// calcuation of MFS
    pContract.MFS = pContract.IRR - fct_MarginNom(lCFProfile)

    
    '// add variable interest cashflows
    If lintVar Then
        lCFProfile.Count2 = 1
        lintRun = LBound(paCF) + 1
        Do While paCF(lintRun, 3) <> 0
            lCFProfile.Count2 = lCFProfile.Count2 + 1
            lCFProfile.a(lCFProfile.Count2).CashFlow = lCFProfile.a(lCFProfile.Count2).CashFlow + paCF(lintRun, 3)
            lintRun = lintRun + 1
        Loop
        lCFProfile.a(lCFProfile.Count2).CashFlow = lCFProfile.a(lCFProfile.Count2).CashFlow + lCFProfile.a(lCFProfile.Count2).NBVnom
        '// calcualtion of IRR
        If fct_IRR(lCFProfile, "30/360") Then
            pContract.IRR = lCFProfile.IRnom
        Else
            Exit Sub
        End If
    End If
    
    '// init cashflow with DCF (yields)
    Call sub_CalcDCF(paYields, pintComp, pstrSW, pstrMM, gDCF, pdblCalcDate, pintAnnualized)
    For lintRun = 1 To lCFProfile.Count2
        With lCFProfile.a(lintRun)
            .DCF = fct_GetDCF(CDbl(.Date), gDCF())
        End With
    Next lintRun
    '// init liquidity runoff with DCF (yields)
    For lintRun = 1 To UBound(pContract.LiqRunoff)
        With pContract.LiqRunoff(lintRun)
            .DCF = fct_GetDCF(CDbl(.Date), gDCF())
        End With
    Next lintRun
    
    '// calcuation of MFR
    pContract.MFR = pContract.IRR - fct_MarginNom(lCFProfile)
    
End Sub

Public Sub sub_CFCompress(CF As typCashflow)
    
    '// delete cashflows=0 and compress on date
    
    Dim lintRun As Integer
    Dim lintPos As Integer
 
    Call sub_CFSort(CF)
    If CF.Count < 2 Then Exit Sub
 
    lintPos = 1
    For lintRun = 2 To CF.Count
        If CF.a(lintRun).CashFlow <> 0 Then
            If CF.a(lintRun).Date = CF.a(lintPos).Date Then
                CF.a(lintPos).CashFlow = CF.a(lintPos).CashFlow + CF.a(lintRun).CashFlow
                If CF.a(lintRun).Flag > CF.a(lintPos).Flag Then
                    CF.a(lintPos).Flag = CF.a(lintRun).Flag
                End If
            Else
                lintPos = lintPos + 1
                If lintPos < lintRun Then CF.a(lintPos) = CF.a(lintRun)
            End If
        End If
    Next lintRun
    CF.Count = lintPos
 
End Sub

Public Sub sub_CFSort(CF As typCashflow)
    
    '// sort CF by date
    
    If CF.Count < 2 Then Exit Sub
    Call sub_CFSortQuic(CF, 1, CF.Count)

End Sub

Private Sub sub_CFSortQuic(CF As typCashflow, ByVal pintLeft As Integer, ByVal pintRight As Integer)
    
    '// sort by date (rekursiv)
    
    Dim l As Integer
    Dim r As Integer
    Dim m As Long
    Dim h As typSingleCF
    
    l = pintLeft
    r = pintRight
    m = CF.a(((l + r) / 2)).Date
    
    Do
        While CF.a(r).Date > m
            r = r - 1
        Wend
        While CF.a(l).Date < m
            l = l + 1
        Wend
        If l < r Then
            h = CF.a(l)
            CF.a(l) = CF.a(r)
            CF.a(r) = h
        End If
        If l <= r Then
            l = l + 1
            r = r - 1
        End If
    Loop Until r < l
 
    If pintLeft < r Then sub_CFSortQuic CF, pintLeft, r
    If l < pintRight Then sub_CFSortQuic CF, l, pintRight

End Sub

Private Sub sub_CFInit(CF As typCashflow, pstrConv As String)
    
    '// init cashflow
    
    Dim lintRun As Integer
    
    With CF
        .IReff = 0
        .IRnom = 0
        If .Count2 < 2 Then Exit Sub
    
        With .a(1)
            .Diff = 0
            .NBVeff = -.CashFlow
            .NBVnom = -.CashFlow
        End With
    
        For lintRun = 2 To .Count2
            With .a(lintRun)
                .Diff = fct_DiffYears(CDbl(CF.a(lintRun - 1).Date), CDbl(.Date), pstrConv)
                .NBVeff = 0
                .NBVnom = 0
            End With
        Next
    End With

End Sub

Private Function fct_IRRNom(CF As typCashflow)
    
    '// calculate nominal IR and NBV (iteration with newton method)
    
    Dim lintSolution As Integer
    Dim lintTry As Integer
    Dim lintRun As Integer
    Dim ldblProd As Double
    Dim ldblSum As Double
    Dim ldblDerivative As Double
    Dim ldblFactor As Double
    
    fct_IRRNom = False
    On Error GoTo err_IRRNom
    
    If CF.Count2 < 2 Then Exit Function
    
    CF.IRnom = 0
    lintSolution = False
    lintTry = 0
    
    Do While Not lintSolution And lintTry < 40
        
        ldblProd = 1
        ldblSum = 0
        ldblDerivative = 0
        
        CF.a(1).NBVnom = -CF.a(1).CashFlow
        For lintRun = 2 To CF.Count2
            With CF.a(lintRun)
                ldblFactor = (1 + CF.IRnom * .Diff)
                .NBVnom = CF.a(lintRun - 1).NBVnom * ldblFactor - .CashFlow
                ldblProd = ldblProd / ldblFactor
                ldblSum = ldblSum + .Diff / ldblFactor
                ldblDerivative = ldblDerivative + .CashFlow * ldblProd * ldblSum
            End With
        Next lintRun
        
        If Abs(CF.a(CF.Count2).NBVnom) < CF.Eps Then
            lintSolution = True
            fct_IRRNom = True
        Else
            lintTry = lintTry + 1
            CF.IRnom = CF.IRnom - CF.a(CF.Count2).NBVnom * ldblProd / ldblDerivative
        End If
        
    Loop

err_IRRNom:

    Exit Function
    
End Function

Public Function fct_IRR(CF As typCashflow, pstrConv As String) As Integer
    
    '// calculate IR and NBV (nominal and effective)
    
    fct_IRR = False
    If CF.Count < 2 Then Exit Function
    
    '// init CF
    Call sub_CFInit(CF, pstrConv)
    
    '// nominal
    If Not fct_IRRNom(CF) Then Exit Function

    '// effective
    CF.IReff = CF.IRnom
    If fct_IRREff(CF) Then
        '// solution with start value nominal rate
        fct_IRR = True
    Else
        '// if no solution try again with start value 0
        CF.IReff = 0
        fct_IRR = fct_IRREff(CF)
    End If

End Function

Private Function fct_IRREff(CF As typCashflow)
    
    '// calculate effective IR and NBV (iteration with newton method)
    
    Dim lintSolution As Integer
    Dim lintTry As Integer
    Dim lintRun As Integer
    Dim ldblProd As Double
    Dim ldblSum As Double
    Dim ldblDerivative As Double
    Dim ldblFactor As Double
    
    fct_IRREff = False
    On Error GoTo err_IRREff
    
    If CF.Count2 < 2 Then Exit Function
    
    lintSolution = False
    lintTry = 0
    
    Do While Not lintSolution And lintTry < 40
        
        ldblProd = 1
        ldblSum = 0
        ldblDerivative = 0
        
        CF.a(1).NBVeff = -CF.a(1).CashFlow
        For lintRun = 2 To CF.Count2
            With CF.a(lintRun)
                ldblFactor = (1 + CF.IReff) ^ .Diff
                .NBVeff = CF.a(lintRun - 1).NBVeff * ldblFactor - .CashFlow
                ldblProd = ldblProd / ldblFactor
                ldblSum = ldblSum + .Diff / (1 + CF.IReff)
                ldblDerivative = ldblDerivative + .CashFlow * ldblProd * ldblSum
            End With
        Next lintRun
        
        If Abs(CF.a(CF.Count2).NBVeff) < CF.Eps Then
            lintSolution = True
            fct_IRREff = True
        Else
            lintTry = lintTry + 1
            CF.IReff = CF.IReff - CF.a(CF.Count2).NBVeff * ldblProd / ldblDerivative
        End If
        
    Loop
    
err_IRREff:

    Exit Function
    
End Function
    




--- Macro File: Tabelle2.cls ---
Attribute VB_Name = "Tabelle2"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "CommandButton1, 3, 0, MSForms, CommandButton"
Attribute VB_Control = "CommandButton2, 4, 1, MSForms, CommandButton"
Private Sub Worksheet_Activate()
Dim i As Integer
Dim intCountdeals As Integer
intCountdeals = 0
Worksheets("BOM Deals").Unprotect Password:="Blattschutz"
Application.ScreenUpdating = False

'Shows only the columns that contain Deal Information, if no deals available columns will be hidden
For i = 4 To 8
    If Worksheets("BOM deals").Cells(7, i).value = "" Then
        Worksheets("BOM deals").Columns(i).Hidden = True
    Else
        Worksheets("BOM deals").Columns(i).Hidden = False
        intCountdeals = intCountdeals + 1
    End If
Next

'Show "Total"-Column only if more than 2 country deals exist
If intCountdeals < 2 Then
    Worksheets("BOM deals").Columns(9).Hidden = True
Else
    Worksheets("BOM deals").Columns(9).Hidden = False
End If

Worksheets("BOM Deals").Range("A1").Activate
Application.ScreenUpdating = True
Worksheets("BOM Deals").Protect Password:="Blattschutz"

End Sub

'Deletes all deals within BOM Deals Sheet
Private Sub CommandButton1_Click()
Application.ScreenUpdating = False
Worksheets("BOM Deals").Unprotect Password:="Blattschutz"

Dim wksBOMDeals As Worksheet
Dim clm As Integer

Set wksBOMDeals = Sheets("BOM Deals")

intSure = MsgBox("Are you sure?", vbOKCancel, "Delete BOM Deals")
If intSure = 2 Then
    Application.Calculate
    Worksheets("BOM Deals").Protect Password:="Blattschutz"
        With Application
            .Calculation = xlAutomatic
            .MaxChange = 0.001
            .ScreenUpdating = True
        End With
        ActiveWorkbook.PrecisionAsDisplayed = True
    Exit Sub
End If
  
wksBOMDeals.Range("D7:H42").Select
Selection.ClearContents
Worksheets("BOM Deals").Range("A1").Select
Worksheets("BOM Deals").Protect Password:="Blattschutz"
Application.ScreenUpdating = True
End Sub
'Opens Portfolio Sheet
Private Sub CommandButton2_Click()
Worksheets("Portfolio").Activate
End Sub




--- Macro File: Frm_Main.frm ---
Attribute VB_Name = "Frm_Main"
Attribute VB_Base = "0{06D20B37-A169-46C1-8B19-0B7C5DCBA86B}{2B4D4474-E6C6-4628-B018-A6D6D582C42B}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Public intEntity_Row As Integer

Private Sub CommandButton2_Click()
Frm_Int_Spr.Show
End Sub

Private Sub CommandButton3_Click()
Frm_Customer.Show
End Sub

Private Sub CommandButton4_Click()
Frm_Opex.Show
End Sub

Private Sub CommandButton5_Click()
Frm_Int_Pd.Show
End Sub

Private Sub CommandButton6_Click()
Frm_Dep_Curve.Show
End Sub

Private Sub CommandButton7_Click()
'Call dataload
Unload Me
End Sub

Private Sub CommandButton8_Click()
Frm_Language.Show
End Sub

Private Sub UserForm_QueryClose(Cancel As Integer, CloseMode As Integer)
    If CloseMode = 0 Then Cancel = True
End Sub

Private Sub UserForm_activate()

intEntity_Row = fct_entity_data_position()

If intEntity_Row = 0 Then
    MsgBox "No Data for selected Entity available"
    Unload Me
End If

End Sub


Private Sub CommandButton1_Click()
Frm_Product.Show
End Sub


--- Macro File: Frm_Product.frm ---
Attribute VB_Name = "Frm_Product"
Attribute VB_Base = "0{CB98BF7C-56A4-415B-A053-F384B637E8C3}{6639EEC5-C15E-4F36-942A-71BD43A88A24}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private strOS_Dec_Separator As String
Private strApp_Dec_Separator As String
Public intEntity_Row As Integer


Private Sub CommandButton1_Click()

Dim intRow_Save As Integer
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

intRow_Save = 1

'Validate entered Product Values
For i = 1 To 30
    If Frm_Product("textbox" & Trim(Str(i))).value <> "" Then
        If Frm_Product("ComboBox" & Trim(Str(i))).value = "" Then
            Frm_Product("ComboBox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a Product Risk Class"
            Frm_Product("ComboBox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Product("textbox" & Trim(Str(i)) & "b").value
        Else
            varTB = Replace(Frm_Product("textbox" & Trim(Str(i)) & "b").value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If Frm_Product("textbox" & Trim(Str(i)) & "b").value <> "" And (Not IsNumeric(Frm_Product("textbox" & Trim(Str(i)) & "b").value) Or varTB < 0 Or varTB > 100) Then
            Frm_Product("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct value"
            Frm_Product("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
    End If
Next

'Delete old Product List
For i = 1 To 30
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posProduct_DE).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posProduct_DE + 1).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posProduct_DE + 2).value = ""
Next


'Save new Products
For i = 1 To 30
    If Frm_Product("textbox" & Trim(Str(i))).value <> "" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posProduct_DE).value = Frm_Product("textbox" & Trim(Str(i))).value
        Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posProduct_DE + 1).value = Frm_Product("ComboBox" & Trim(Str(i))).value
        If IsNumeric(Frm_Product("textbox" & Trim(Str(i)) & "b").value) Then
            
            If Application.UseSystemSeparators = True Then
                varTB = Frm_Product("textbox" & Trim(Str(i)) & "b").value
            Else
                varTB = Replace(Frm_Product("textbox" & Trim(Str(i)) & "b").value, strApp_Dec_Separator, strOS_Dec_Separator)
            End If
            Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posProduct_DE + 2).value = CDbl(varTB)
        Else
            Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posProduct_DE + 2).value = ""
        End If
            intRow_Save = intRow_Save + 1
    End If
Next

'MsgBox "Product List Update Succesful"

Unload Me

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

Private Sub UserForm_activate()

Dim i As Integer

intEntity_Row = fct_entity_data_position()
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

If intEntity_Row = 0 Then
    MsgBox "No Data for selected Entity available"
    Unload Me
End If

For i = 1 To 30
    Frm_Product("textbox" & Trim(Str(i))).value = Worksheets("Data_Entities").Cells(intEntity_Row + i, posProduct_DE).value
    Frm_Product("ComboBox" & Trim(Str(i))).value = Worksheets("Data_Entities").Cells(intEntity_Row + i, posProduct_DE + 1).value
    If Application.UseSystemSeparators = True Then
        Frm_Product("textbox" & Trim(Str(i)) & "b").text = Worksheets("Data_Entities").Cells(intEntity_Row + i, posProduct_DE + 2).value
    Else
        Frm_Product("textbox" & Trim(Str(i)) & "b").text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + i, posProduct_DE + 2).value, strOS_Dec_Separator, strApp_Dec_Separator)
    End If
Next


End Sub




--- Macro File: Frm_Int_Spr.frm ---
Attribute VB_Name = "Frm_Int_Spr"
Attribute VB_Base = "0{72D1F754-03E1-4888-80D3-95B53A1458FD}{C68BD77A-047E-4BCA-AA24-61038688F061}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Public intEntity_Row As Integer
Private strOS_Dec_Separator As String
Private strApp_Dec_Separator As String

Private Sub CheckBox1_change()
If CheckBox1.value = True Then
    ComboBox2.Visible = True
    ComboBox3.Visible = True
    ComboBox4.Visible = True
    ComboBox5.Visible = True
    Label71.Visible = True
    Label72.Visible = True
    Label74.Visible = True
    Label75.Visible = True
Else
    ComboBox2.Visible = False
    ComboBox3.Visible = False
    ComboBox4.Visible = False
    ComboBox5.Visible = False
    Label71.Visible = False
    Label72.Visible = False
    Label74.Visible = False
    Label75.Visible = False
End If
End Sub

Private Sub CheckBox2_Click()
If CheckBox2.value = True Then
    ComboBox6.Visible = True
    ComboBox7.Visible = True
    ComboBox8.Visible = True
    ComboBox9.Visible = True
    Label79.Visible = True
    Label80.Visible = True
    Label81.Visible = True
    Label82.Visible = True
Else
    ComboBox6.Visible = False
    ComboBox7.Visible = False
    ComboBox8.Visible = False
    ComboBox9.Visible = False
    Label79.Visible = False
    Label80.Visible = False
    Label81.Visible = False
    Label82.Visible = False
End If
End Sub


Private Sub CheckBox3_Click()
If CheckBox3.value = True Then
    ComboBox11.Visible = True
    ComboBox12.Visible = True
    ComboBox13.Visible = True
    ComboBox14.Visible = True
    Label86.Visible = True
    Label87.Visible = True
    Label88.Visible = True
    Label83.Visible = True
Else
    ComboBox11.Visible = False
    ComboBox12.Visible = False
    ComboBox13.Visible = False
    ComboBox14.Visible = False
    Label86.Visible = False
    Label87.Visible = False
    Label88.Visible = False
    Label83.Visible = False
End If
End Sub

Private Sub CheckBox6_Click()

End Sub

Private Sub CommandButton1_Click()
Dim i As Integer
Dim intColumn_Save As Integer
intColumn_Save = 0

'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'Validate First Interests and Spreads Curve
If ComboBox1.value <> "" Then
    If ComboBox2.value = "" Then
        Frm_Int_Spr("combobox2").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Day Convention Money Market Value"
        Frm_Int_Spr("combobox2").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox3.value = "" Then
         Frm_Int_Spr("combobox3").BackColor = RGB(255, 0, 0)
         MsgBox "Please add a Day Convention Swap Market Value"
         Frm_Int_Spr("combobox3").BackColor = RGB(255, 255, 255)
         Exit Sub
    ElseIf ComboBox4.value = "" Then
        Frm_Int_Spr("ComboBox4").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Compounding Frequency"
        Frm_Int_Spr("ComboBox4").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox5.value = "" Then
        Frm_Int_Spr("ComboBox5").BackColor = RGB(255, 0, 0)
        MsgBox "Please add if annualized"
        Frm_Int_Spr("ComboBox5").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf TextBox67.value = "" Then
        Frm_Int_Spr("TextBox67").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Quotation Date"
        Frm_Int_Spr("TextBox67").BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
        
    For i = 1 To 22
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Spr("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Spr("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If (Not IsNumeric(Frm_Int_Spr("textbox" & Trim(Str(i))).value)) And Frm_Int_Spr("textbox" & Trim(Str(i))).value <> "" Then
            Frm_Int_Spr("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct value"
            Frm_Int_Spr("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
End If

'Validate Second Interests and Spreads Curve
If ComboBox10.value <> "" Then
    If ComboBox9.value = "" Then
        Frm_Int_Spr("combobox9").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Day Convention Money Market Value"
        Frm_Int_Spr("combobox9").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox8.value = "" Then
         Frm_Int_Spr("ComboBox8").BackColor = RGB(255, 0, 0)
         MsgBox "Please add a Day Convention Swap Market Value"
         Frm_Int_Spr("ComboBox8").BackColor = RGB(255, 255, 255)
         Exit Sub
    ElseIf ComboBox7.value = "" Then
        Frm_Int_Spr("ComboBox7").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Compounding Frequency"
        Frm_Int_Spr("ComboBox7").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox6.value = "" Then
        Frm_Int_Spr("ComboBox6").BackColor = RGB(255, 0, 0)
        MsgBox "Please add if annualized"
        Frm_Int_Spr("ComboBox6").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf TextBox68.value = "" Then
        Frm_Int_Spr("TextBox68").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Quotation Date"
        Frm_Int_Spr("TextBox68").BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
        
    For i = 23 To 44
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Spr("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Spr("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If (Not IsNumeric(Frm_Int_Spr("textbox" & Trim(Str(i))).value)) And Frm_Int_Spr("textbox" & Trim(Str(i))).value <> "" Then
            Frm_Int_Spr("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct value"
            Frm_Int_Spr("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
      End If
    Next
End If

'Validate Third Interests and Spreads Curve
If ComboBox15.value <> "" Then
    If ComboBox14.value = "" Then
        Frm_Int_Spr("ComboBox14").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Day Convention Money Market Value"
        Frm_Int_Spr("ComboBox14").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox13.value = "" Then
        Frm_Int_Spr("ComboBox13").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Day Convention Swap Market Value"
        Frm_Int_Spr("ComboBox13").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox12.value = "" Then
        Frm_Int_Spr("ComboBox12").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Compounding Frequency"
        Frm_Int_Spr("ComboBox12").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox11.value = "" Then
        Frm_Int_Spr("ComboBox11").BackColor = RGB(255, 0, 0)
        MsgBox "Please add if annualized"
        Frm_Int_Spr("ComboBox11").BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf TextBox69.value = "" Then
        Frm_Int_Spr("TextBox69").BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Quotation Date"
        Frm_Int_Spr("TextBox69").BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
        
    For i = 45 To 66
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Spr("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Spr("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If (Not IsNumeric(Frm_Int_Spr("textbox" & Trim(Str(i))).value)) And Frm_Int_Spr("textbox" & Trim(Str(i))).value <> "" Then
            Frm_Int_Spr("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct value"
            Frm_Int_Spr("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
      End If
    Next
End If

'Delete old Interest and Spreads
For i = 1 To 29
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntSpr_DE).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntSpr_DE + 1).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntSpr_DE + 2).value = ""
Next

'Save First Interests and Spreads Curve
If ComboBox1.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + intColumn_Save).value = ComboBox1.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + intColumn_Save).value = ComboBox3.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + intColumn_Save).value = ComboBox2.value
        
    Select Case ComboBox4.value
        Case "Monthly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "1"
        Case "Quarterly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "3"
        Case "Annual"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "12"
        Case Else
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "6"
    End Select
            
    If ComboBox5.value = "No" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "0"
    Else
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "-1"
    End If
    
    Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + intColumn_Save).value = CDate(TextBox67.value)
    
    For i = 1 To 22
        If Frm_Int_Spr("textbox" & Trim(Str(i))).value = "" Then
            Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE + intColumn_Save).value = Frm_Int_Spr("textbox" & Trim(Str(i))).value
        Else
            If Application.UseSystemSeparators = True Then
                varTB = Frm_Int_Spr("textbox" & Trim(Str(i))).value
            Else
                varTB = Replace(Frm_Int_Spr("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
            End If
            Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE + intColumn_Save).value = CDbl(varTB)
        End If
    Next
    intColumn_Save = intColumn_Save + 1
End If

'Save Second Interests and Spreads Curve
If ComboBox10.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + intColumn_Save).value = ComboBox10.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + intColumn_Save).value = ComboBox9.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + intColumn_Save).value = ComboBox8.value
    Select Case ComboBox7.value
        Case "Monthly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "1"
        Case "Quarterly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "3"
        Case "Annual"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "12"
        Case Else
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "6"
    End Select

    If ComboBox6.value = "No" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "0"
    Else
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "-1"
    End If
    
    Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + intColumn_Save).value = CDate(TextBox68.value)
    
    For i = 23 To 44
        If Frm_Int_Spr("textbox" & Trim(Str(i))).value = "" Then
            Worksheets("Data_Entities").Cells(intEntity_Row - 20 + i, posIntSpr_DE + intColumn_Save).value = Frm_Int_Spr("textbox" & Trim(Str(i))).value
        Else
            If Application.UseSystemSeparators = True Then
                varTB = Frm_Int_Spr("textbox" & Trim(Str(i))).value
            Else
                varTB = Replace(Frm_Int_Spr("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
            End If
            Worksheets("Data_Entities").Cells(intEntity_Row - 20 + i, posIntSpr_DE + intColumn_Save).value = CDbl(varTB)
        End If
    Next
    intColumn_Save = intColumn_Save + 1
End If

'Save Third Interests and Spreads Curve
If ComboBox15.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + intColumn_Save).value = ComboBox15.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + intColumn_Save).value = ComboBox14.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + intColumn_Save).value = ComboBox13.value

    Select Case ComboBox12.value
        Case "Monthly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "1"
        Case "Quarterly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "3"
        Case "Annual"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "12"
        Case Else
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "6"
    End Select

    If ComboBox11.value = "No" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "0"
    Else
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "-1"
    End If
    
    Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + intColumn_Save).value = CDate(TextBox69.value)
    
    For i = 45 To 66
        If Frm_Int_Spr("textbox" & Trim(Str(i))).value = "" Then
            Worksheets("Data_Entities").Cells(intEntity_Row - 42 + i, posIntSpr_DE + intColumn_Save).value = Frm_Int_Spr("textbox" & Trim(Str(i))).value
        Else
            If Application.UseSystemSeparators = True Then
                varTB = Frm_Int_Spr("textbox" & Trim(Str(i))).value
            Else
                varTB = Replace(Frm_Int_Spr("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
            End If
            Worksheets("Data_Entities").Cells(intEntity_Row - 42 + i, posIntSpr_DE + intColumn_Save).value = CDbl(varTB)
        End If
    Next
End If

'MsgBox "Interests and Spreads Update Succesful"
Unload Me
End Sub


Private Sub CommandButton3_Click()
Frm_date_is1.Show
End Sub

Private Sub CommandButton4_Click()
Frm_date_is2.Show
End Sub

Private Sub CommandButton5_Click()
Frm_date_is3.Show
End Sub





Private Sub UserForm_activate()

ComboBox2.Visible = False
ComboBox3.Visible = False
ComboBox4.Visible = False
ComboBox5.Visible = False
Label71.Visible = False
Label72.Visible = False
Label74.Visible = False
Label75.Visible = False
   
ComboBox6.Visible = False
ComboBox7.Visible = False
ComboBox8.Visible = False
ComboBox9.Visible = False
Label79.Visible = False
Label80.Visible = False
Label81.Visible = False
Label82.Visible = False

ComboBox11.Visible = False
ComboBox12.Visible = False
ComboBox13.Visible = False
ComboBox14.Visible = False
Label86.Visible = False
Label87.Visible = False
Label88.Visible = False
Label83.Visible = False

Dim i As Integer

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator
intEntity_Row = fct_entity_data_position()

'Load First Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE).value <> "" Then
    ComboBox1.value = Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE).value
    ComboBox2.value = Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE).value
    ComboBox3.value = Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE).value
    Select Case Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE).value
        Case "1"
            ComboBox4.value = "Monthly"
        Case "3"
            ComboBox4.value = "Quarterly"
        Case "12"
            ComboBox4.value = "Annual"
        Case Else
            ComboBox4.value = "Semi-annual"
    End Select
            
    If Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE).value = "0" Then
        ComboBox5.value = "No"
    Else
        ComboBox5.value = "Yes"
    End If
    TextBox67.value = Format(Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE).value)
    For i = 1 To 22
        If Application.UseSystemSeparators = True Then
            Frm_Int_Spr("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE).value
        Else
            Frm_Int_Spr("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    Next
End If

'Load Second Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + 1).value <> "" Then
    ComboBox10.value = Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + 1).value
    ComboBox9.value = Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + 1).value
    ComboBox8.value = Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + 1).value
    Select Case Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + 1).value
        Case "1"
            ComboBox7.value = "Monthly"
        Case "3"
            ComboBox7.value = "Quarterly"
        Case "12"
            ComboBox7.value = "Annual"
        Case Else
            ComboBox7.value = "Semi-annual"
    End Select
            
    If Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + 1).value = "0" Then
        ComboBox6.value = "No"
    Else
        ComboBox6.value = "Yes"
    End If
    TextBox68.value = Format(Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + 1).value)
    For i = 23 To 44
        If Application.UseSystemSeparators = True Then
            Frm_Int_Spr("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row - 20 + i, posIntSpr_DE + 1).value
        Else
            Frm_Int_Spr("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row - 20 + i, posIntSpr_DE + 1).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    Next
End If

'Load Third Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + 2).value <> "" Then
    ComboBox15.value = Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + 2).value
    ComboBox14.value = Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + 2).value
    ComboBox13.value = Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + 2).value
    
       Select Case Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + 2).value
        Case "1"
            ComboBox12.value = "Monthly"
        Case "3"
            ComboBox12.value = "Quarterly"
        Case "12"
            ComboBox12.value = "Annual"
        Case Else
            ComboBox12.value = "Semi-annual"
    End Select
    
    If Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + 2).value = "0" Then
        ComboBox11.value = "No"
    Else
        ComboBox11.value = "Yes"
    End If
    TextBox69.value = Format(Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + 2).value)
    For i = 45 To 66
        If Application.UseSystemSeparators = True Then
            Frm_Int_Spr("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row - 42 + i, posIntSpr_DE + 2).value
        Else
            Frm_Int_Spr("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row - 42 + i, posIntSpr_DE + 2).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    Next
End If

End Sub


Private Sub CommandButton2_Click()
Unload Me
End Sub


--- Macro File: Sheet1.cls ---
Attribute VB_Name = "Sheet1"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Option Explicit



--- Macro File: Library_Yield_Curve_DCF.bas ---
Attribute VB_Name = "Library_Yield_Curve_DCF"
Option Explicit

'// calculation of zerobond discount factors
'// curve input required as dynamic array of type typCurveInput (only relevant items are filled)

'// example:
'// Dim laCurve(0 To 12) As typCurveInput
'// laCurve(0).Type = "1M"
'// laCurve(0).Yield = 4.5
'// laCurve(1).Type = "2M"
'// laCurve(1).Yield = 5.1
'// ....

Global Const cMaxMonths = 360

Public Type typConv
    Type As String
    DayCount As String
    Compound As Integer
End Type

Public Type typCalcDCF
    Yield As Double
    Date As Double
    DCF As Double
    Type As String
    DayCount As String
    Compound As Integer
End Type

Public Type typDCF
    Date As Double
    DCF As Double
End Type

Public Type typCurveInput
    Type As String
    Yield As Double
End Type

Global gDCF() As typDCF

Function fct_GetDCF(pdblDate As Double, paDCF() As typDCF) As Double
 
    '// calculate day accurate discount factor by exponential interpolation
    
    Dim lintRun As Integer
    Dim ldblHelp As Double
    
    ldblHelp = paDCF(LBound(paDCF)).Date
    If pdblDate < ldblHelp Then
        fct_GetDCF = 0
        Exit Function
    End If
    
    If pdblDate < paDCF(LBound(paDCF) + 1).Date Then
        If pdblDate = paDCF(LBound(paDCF)).Date Then
            fct_GetDCF = paDCF(LBound(paDCF)).DCF
        Else
            fct_GetDCF = fct_Linear(pdblDate - ldblHelp, 1, paDCF(LBound(paDCF) + 1).DCF, 0, paDCF(LBound(paDCF) + 1).Date - ldblHelp)
        End If
        Exit Function
    End If
    
    lintRun = UBound(paDCF)
    If pdblDate >= paDCF(lintRun).Date Then
        fct_GetDCF = 1 / ((1 / paDCF(lintRun).DCF) ^ ((pdblDate - ldblHelp) / (paDCF(lintRun).Date - ldblHelp)))
        Exit Function
    End If
    
    For lintRun = LBound(paDCF) + 1 To UBound(paDCF)
        If pdblDate >= paDCF(lintRun).Date And pdblDate < paDCF(lintRun + 1).Date Then
            If pdblDate = paDCF(lintRun).Date Then
                fct_GetDCF = paDCF(lintRun).DCF
            Else
                fct_GetDCF = fct_Exponential(pdblDate - ldblHelp, paDCF(lintRun).DCF, paDCF(lintRun + 1).DCF, paDCF(lintRun).Date - ldblHelp, paDCF(lintRun + 1).Date - ldblHelp)
            End If
            Exit Function
        End If
    Next lintRun
    
End Function


Function fct_GetYieldNom(pdblEnd As Double, pstrMM As String, pstrSW As String, pintComp As Integer, pdblDate As Double, Optional pdblStart As Double = 0) As Double
 
    '// returns the nominal rate according to the day count conventions
    
    Dim ldblHelp As Double
    Dim ldblDCF As Double
    Dim ldblSum As Double
    Dim ldblLast As Double
    Dim ldblNext As Double
    
    fct_GetYieldNom = 0
    
    ldblHelp = IIf(pdblStart = 0, pdblDate, pdblStart)
    If pdblEnd = ldblHelp Then Exit Function
    
    If pdblEnd <= DateAdd("m", 12, ldblHelp) Then
        '// money market
        ldblDCF = fct_GetDCF(pdblEnd, gDCF()) / fct_GetDCF(ldblHelp, gDCF())
        If Abs(ldblDCF) < 0.0000001 Then
            fct_GetYieldNom = 0
        Else
            fct_GetYieldNom = (1 / ldblDCF - 1) * 100 / fct_DiffYears(ldblHelp, pdblEnd, pstrMM)
        End If
    Else
        '// swap market
        ldblSum = 0
        ldblNext = pdblEnd
        ldblLast = DateAdd("m", -pintComp, ldblNext)
        Do While ldblLast > ldblHelp
            ldblDCF = fct_GetDCF(ldblNext, gDCF()) / fct_GetDCF(ldblHelp, gDCF())
            ldblSum = ldblSum + ldblDCF * fct_DiffYears(ldblLast, ldblNext, pstrSW)
            ldblNext = ldblLast
            ldblLast = DateAdd("m", -pintComp, ldblNext)
        Loop
        ldblDCF = fct_GetDCF(ldblNext, gDCF()) / fct_GetDCF(ldblHelp, gDCF())
        ldblSum = ldblSum + ldblDCF * fct_DiffYears(ldblHelp, ldblNext, pstrSW)
  
        If Abs(ldblSum) < 0.0000001 Then
            fct_GetYieldNom = 0
        Else
            ldblDCF = fct_GetDCF(pdblEnd, gDCF()) / fct_GetDCF(ldblHelp, gDCF())
            fct_GetYieldNom = (1 - ldblDCF) / ldblSum * 100
        End If
    End If
    
End Function


Public Sub sub_CalcDCF(paCurve() As typCurveInput, ByVal plngComp As Long, ByVal pstrSW As String, ByVal pstrMM As String, padblDCF() As typDCF, pdblDate As Double, pintAnnualized As Integer)

    Dim ladblData() As Double
    Dim ladblDays() As Double
    Dim latypConv() As typConv
    Dim ladblDataSpreads() As Double
    Dim ladblDaysSpreads() As Double
    Dim lintFound As Integer
 
    Dim laTemp(-1 To cMaxMonths) As typCalcDCF
    
    Dim lintRun As Integer
    Dim lintCount As Integer
    
    '// read yield or spread data
    Call sub_ReadYields(paCurve(), plngComp, ladblData(), ladblDays(), latypConv(), pdblDate, pintAnnualized)
    '// interpolation of missing yields for all defined gridpoints (in Focus)
    Call sub_FillGridpoints(ladblData(), ladblDays(), latypConv(), pdblDate)
    
    '// conventions of rates
    For lintRun = LBound(latypConv) To UBound(latypConv)
        latypConv(lintRun).DayCount = IIf(latypConv(lintRun).Type = "MM", pstrMM, pstrSW)
        latypConv(lintRun).Compound = IIf(latypConv(lintRun).Type = "MM", 0, plngComp)
    Next lintRun
    
    '// insert neccessary dates into array
    For lintRun = plngComp To cMaxMonths Step plngComp
        laTemp(lintRun).Date = DateAdd("m", lintRun, pdblDate)
        laTemp(lintRun).Yield = -1
    Next lintRun
    
    '// initialize array with yield curve data
    For lintRun = 0 To UBound(ladblDays)
        lintCount = fct_DiffDays30(pdblDate + 1, ladblDays(lintRun) + 1)
        Select Case lintCount
            Case 1, 2
                laTemp(-1).Yield = ladblData(lintRun) / 100
                laTemp(-1).Date = ladblDays(lintRun)
                laTemp(-1).Type = latypConv(lintRun).Type
                laTemp(-1).DayCount = latypConv(lintRun).DayCount
                laTemp(-1).Compound = latypConv(lintRun).Compound
            Case 7
                laTemp(0).Yield = ladblData(lintRun) / 100
                laTemp(0).Date = ladblDays(lintRun)
                laTemp(0).Type = latypConv(lintRun).Type
                laTemp(0).DayCount = latypConv(lintRun).DayCount
                laTemp(0).Compound = latypConv(lintRun).Compound
            Case Else
                laTemp(lintCount / 30).Yield = ladblData(lintRun) / 100
                laTemp(lintCount / 30).Date = ladblDays(lintRun)
                laTemp(lintCount / 30).Type = latypConv(lintRun).Type
                laTemp(lintCount / 30).DayCount = latypConv(lintRun).DayCount
                laTemp(lintCount / 30).Compound = latypConv(lintRun).Compound
        End Select
    Next lintRun
    
    '// all markets with specific definitions
    Call sub_CalcAllMarkets(laTemp(), pdblDate)
 
    '// save dcf in result array
    ReDim padblDCF(0)
    padblDCF(0).Date = pdblDate
    padblDCF(0).DCF = 1
    lintCount = 0
    For lintRun = -1 To cMaxMonths
        If laTemp(lintRun).DCF <> 0 Then
            lintCount = lintCount + 1
            ReDim Preserve padblDCF(lintCount)
            padblDCF(lintCount).Date = laTemp(lintRun).Date
            padblDCF(lintCount).DCF = laTemp(lintRun).DCF
        End If
    Next lintRun
    
End Sub
Public Sub sub_CalcAllMarkets(laCalc() As typCalcDCF, pdblStart As Double)
    
    Dim lintRun As Integer
    Dim lintCheck As Integer
    Dim lintCount As Integer
    Dim lintLast As Integer
    Dim lintNext As Integer
    Dim ldblSum As Double
    Dim ldblDate As Double
    
    For lintRun = -1 To cMaxMonths
        '// DCF calculation
        If laCalc(lintRun).Date > 0 And laCalc(lintRun).Yield <> -1 Then
            Select Case laCalc(lintRun).Type
                Case "MM"
                    laCalc(lintRun).DCF = 1 / (1 + laCalc(lintRun).Yield * fct_DiffYears(pdblStart, laCalc(lintRun).Date, laCalc(lintRun).DayCount))
                Case "Swap"
                    ldblSum = 0
                    ldblDate = pdblStart
                    For lintCount = laCalc(lintRun).Compound To lintRun Step laCalc(lintRun).Compound
                        If laCalc(lintCount).DCF = 0 Then
                            '// interpolation of yield and discount factor for bootstrapping
                            lintLast = lintCount - laCalc(lintRun).Compound
                            lintNext = lintRun
                            laCalc(lintCount).Yield = fct_Linear(laCalc(lintCount).Date, laCalc(lintLast).Yield, laCalc(lintNext).Yield, laCalc(lintLast).Date, laCalc(lintNext).Date)
                            laCalc(lintCount).DCF = (1 - laCalc(lintCount).Yield * ldblSum) / (1 + laCalc(lintCount).Yield * fct_DiffYears(ldblDate, laCalc(lintCount).Date, laCalc(lintRun).DayCount))
                        End If
                        ldblSum = ldblSum + laCalc(lintCount).DCF * fct_DiffYears(ldblDate, laCalc(lintCount).Date, laCalc(lintRun).DayCount)
                        ldblDate = laCalc(lintCount).Date
                    Next lintCount
                Case "Zero"
                    laCalc(lintRun).DCF = 1 / (1 + laCalc(lintRun).Yield) ^ fct_DiffYears(pdblStart, laCalc(lintRun).Date, laCalc(lintRun).DayCount)
            End Select
            If laCalc(lintRun).Type <> "Swap" And lintRun > -1 Then
                '// interpolation of missing gridpoints for bootstrapping
                lintCheck = lintRun - 1
                Do While lintCheck > 3
                    If laCalc(lintCheck).Date > 0 And laCalc(lintCheck).DCF = 0 Then
                        '// last
                        For lintCount = lintCheck To -1 Step -1
                            If laCalc(lintCount).DCF <> 0 Then
                                lintLast = lintCount
                                Exit For
                            End If
                        Next lintCount
                        '// next
                        lintNext = lintRun
                        '// interpolation
                        If lintNext > lintCheck Then
                            laCalc(lintCheck).DCF = fct_Exponential(laCalc(lintCheck).Date - pdblStart, laCalc(lintLast).DCF, laCalc(lintNext).DCF, laCalc(lintLast).Date - pdblStart, laCalc(lintNext).Date - pdblStart)
                        End If
                    End If
                    lintCheck = lintCheck - 1
                Loop
            End If
        End If
    Next lintRun
    
End Sub


Public Sub sub_FillGridpoints(pdblYields() As Double, pdblDates() As Double, ptypConv() As typConv, pdblStart As Double)

    Dim ladblTempYields(16) As Double
    Dim ladblTempDates(16) As Double
    Dim latypTempConv(16) As typConv
    Dim lintRun As Integer
    Dim lintCount As Integer

    '// initialize gridpoints 1d, 1w, 1m, 2m, 3m, 6m, 12m, 2y, 3y, 4y, 5y, 7y, 10y, 12y, 15y, 20y, 30y
    ladblTempDates(0) = pdblStart + 1
    ladblTempDates(1) = pdblStart + 7
    For lintRun = 1 To 3
        ladblTempDates(lintRun + 1) = DateAdd("m", lintRun, pdblStart)
    Next lintRun
    ladblTempDates(5) = DateAdd("m", 6, pdblStart)
    For lintRun = 1 To 5
        ladblTempDates(lintRun + 5) = DateAdd("m", lintRun * 12, pdblStart)
    Next lintRun
    ladblTempDates(11) = DateAdd("m", 7 * 12, pdblStart)
    ladblTempDates(12) = DateAdd("m", 10 * 12, pdblStart)
    ladblTempDates(13) = DateAdd("m", 12 * 12, pdblStart)
    ladblTempDates(14) = DateAdd("m", 15 * 12, pdblStart)
    ladblTempDates(15) = DateAdd("m", 20 * 12, pdblStart)
    ladblTempDates(16) = DateAdd("m", 30 * 12, pdblStart)

    '// linear interpolation of missing yields for gridpoints
    For lintRun = LBound(ladblTempDates) To UBound(ladblTempDates)
        lintCount = lintRun
        If lintCount > UBound(pdblDates) Then lintCount = UBound(pdblDates)
        Do While pdblDates(lintCount) > ladblTempDates(lintRun)
            lintCount = lintCount - 1
            If lintCount = 0 Then Exit Do
        Loop
        If lintCount = UBound(pdblDates) Then
            ladblTempYields(lintRun) = pdblYields(lintCount)
        Else
            ladblTempYields(lintRun) = fct_Linear(ladblTempDates(lintRun), pdblYields(lintCount), pdblYields(lintCount + 1), pdblDates(lintCount), pdblDates(lintCount + 1))
        End If
    Next lintRun
    
    '// convention type
    For lintRun = 0 To 6
        latypTempConv(lintRun).Type = "MM"
    Next lintRun
    For lintRun = 7 To 16
        latypTempConv(lintRun).Type = "Swap"
    Next lintRun
    
    pdblDates = ladblTempDates
    pdblYields = ladblTempYields
    ptypConv = latypTempConv
        
End Sub


Private Sub sub_ReadYields(paInput() As typCurveInput, ByVal pintComp As Integer, pdblYields() As Double, pdblDates() As Double, ptypConv() As typConv, pdblStart As Double, pintAnnualized As Integer)

    Dim ldblHelp As Double
    Dim lintRun As Integer
    Dim lintInput As Integer
    
    lintInput = LBound(paInput)
    If paInput(lintInput).Type = "1D" Then lintInput = lintInput + 1
    lintRun = 1
    
    '// one week
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "1W" Then
            ldblHelp = paInput(lintInput).Yield
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = pdblStart + 7
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "MM"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// one month
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "1M" Then
            ldblHelp = paInput(lintInput).Yield
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 1, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "MM"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 2 months
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "2M" Then
            ldblHelp = paInput(lintInput).Yield
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 2, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "MM"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 3 months
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "3M" Then
            ldblHelp = paInput(lintInput).Yield
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 3, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "MM"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 6 months
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "6M" Then
            ldblHelp = paInput(lintInput).Yield
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 6, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "MM"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 12 months
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "12M" Then
            ldblHelp = paInput(lintInput).Yield
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 12, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "MM"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 2 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "2Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 24, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 3 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "3Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 36, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 4 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "4Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 48, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 5 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "5Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 60, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 7 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "7Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 84, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 10 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "10Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 120, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 12 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "12Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 144, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 15 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "15Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 180, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 20 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "20Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 240, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 30 years
    If lintInput <= UBound(paInput) Then
        If paInput(lintInput).Type = "30Y" Then
            ldblHelp = paInput(lintInput).Yield
            If pintAnnualized Then
                ldblHelp = fct_YieldEff2Nom(ldblHelp, pintComp)
            End If
            ReDim Preserve pdblYields(lintRun)
            pdblYields(lintRun) = ldblHelp
            ReDim Preserve pdblDates(lintRun)
            pdblDates(lintRun) = DateAdd("m", 360, pdblStart)
            ReDim Preserve ptypConv(lintRun)
            ptypConv(lintRun).Type = "SW"
            lintRun = lintRun + 1
            lintInput = lintInput + 1
        End If
    End If
    
    '// 1 day, if is NULL fill with first available
    If paInput(LBound(paInput)).Type = "1D" Then
        ldblHelp = paInput(LBound(paInput)).Yield
    Else
        ldblHelp = pdblYields(1)
    End If
    pdblYields(0) = ldblHelp
    pdblDates(0) = pdblStart + 1
    ptypConv(0).Type = "MM"

End Sub

Function fct_YieldEff2Nom(ldblYield As Double, pintPeriod As Integer) As Double
    
    '// convert an effective into a nominal rate
    
    fct_YieldEff2Nom = 0
    
    If pintPeriod = 12 Then
        fct_YieldEff2Nom = ldblYield
    Else
        If ldblYield <= 0 Then Exit Function
        fct_YieldEff2Nom = ((1 + ldblYield / 100) ^ (pintPeriod / 12) - 1) * (12 / pintPeriod) * 100
    End If
    
End Function


Function fct_YieldNom2Eff(pdblYield As Double, pintPeriod As Integer) As Double
    
    '// convert a nominal into an effective rate
 
    fct_YieldNom2Eff = 0
    
    If pintPeriod = 12 Then
        fct_YieldNom2Eff = pdblYield
    Else
        If pdblYield <= 0 Then Exit Function
        fct_YieldNom2Eff = ((1 + pdblYield / (1200 / pintPeriod)) ^ (12 / pintPeriod) - 1) * 100
    End If
    
End Function


Public Sub sub_CalcMoneyMarket(laCalc() As typCalcDCF, plngComp As Long, pstrDayCount As String, pdblStart As Double)
    
    Dim lintRun As Integer
    Dim lintCount As Integer
    Dim lintLast As Integer
    Dim lintNext As Integer
    Dim ldblDate As Double
    Dim ldblYield As Double
    
    '// calculation for available yields
    For lintRun = -1 To 12
        If laCalc(lintRun).Date > 0 And laCalc(lintRun).Yield <> -1 Then
            laCalc(lintRun).DCF = 1 / (1 + laCalc(lintRun).Yield * fct_DiffYears(pdblStart, laCalc(lintRun).Date, pstrDayCount))
        End If
    Next lintRun
    
    '// interpolation of missing ones
    For lintRun = 0 To 11
        If laCalc(lintRun).Date > 0 And laCalc(lintRun).DCF = 0 Then
            '// last
            For lintCount = lintRun To -1 Step -1
                If laCalc(lintCount).DCF <> 0 Then
                    lintLast = lintCount
                    Exit For
                End If
            Next lintCount
            '// next
            lintNext = lintLast
            For lintCount = lintRun To 12
                If laCalc(lintCount).DCF <> 0 Then
                    lintNext = lintCount
                    Exit For
                End If
            Next lintCount
            '// interpolation
            If lintNext > lintRun Then
                laCalc(lintRun).DCF = fct_Exponential(laCalc(lintRun).Date - pdblStart, laCalc(lintLast).DCF, laCalc(lintNext).DCF, laCalc(lintLast).Date - pdblStart, laCalc(lintNext).Date - pdblStart)
            End If
        End If
    Next lintRun
    
    '// interpolate first cornerstone for bootstrapping if still needed
    If laCalc(plngComp).Date > 0 And laCalc(plngComp).DCF = 0 Then
        For lintCount = plngComp - 1 To -1 Step -1
            If laCalc(lintCount).Date > 0 And laCalc(lintCount).Yield <> -1 Then
                lintLast = lintCount
                Exit For
            End If
        Next lintCount
        lintNext = lintLast
        For lintCount = plngComp + 1 To UBound(laCalc)
            If laCalc(lintCount).Date > 0 And laCalc(lintCount).Yield <> -1 Then
                lintNext = lintCount
                Exit For
            End If
        Next lintCount
        If lintNext = lintLast Then
            ldblYield = laCalc(lintLast).Yield
        Else
            ldblYield = fct_Linear(plngComp, laCalc(lintLast).Yield, laCalc(lintNext).Yield, lintLast, lintNext)
        End If
        laCalc(plngComp).DCF = 1 / (1 + ldblYield * fct_DiffYears(pdblStart, laCalc(plngComp).Date, pstrDayCount))
    End If

End Sub

Public Sub sub_CalcSwapMarket(laCalc() As typCalcDCF, plngComp As Long, pstrDayCount As String, pdblStart As Double)

    Dim lintRun As Integer
    Dim lintCount As Integer
    Dim lintMM As Integer
    Dim lintLast As Integer
    Dim lintNext As Integer
    Dim ldblSum As Double
    Dim ldblDate As Double
    
    '// sum of dcf from money market
    ldblDate = pdblStart
    For lintRun = plngComp To 12 Step plngComp
        If laCalc(lintRun).DCF <> 0 Then
            lintMM = lintRun
            ldblSum = ldblSum + laCalc(lintRun).DCF * fct_DiffYears(ldblDate, laCalc(lintRun).Date, pstrDayCount)
            ldblDate = laCalc(lintRun).Date
        End If
    Next lintRun
    
    '// calculate swap rate of last available money market rate
    laCalc(lintMM).Yield = (1 - laCalc(lintMM).DCF) / ldblSum
    
    '// check the 30 year rate
    If laCalc(cMaxMonths).Yield = -1 Then
        For lintRun = cMaxMonths To lintMM Step -1
            If laCalc(lintRun).Date > 0 And laCalc(lintRun).Yield <> -1 Then
                laCalc(cMaxMonths).Yield = laCalc(lintRun).Yield
                Exit For
            End If
        Next lintRun
    End If
    
    '// interpolation of yields that are needed for bootstrapping
    lintCount = lintMM + plngComp
    Do While lintCount < cMaxMonths
        If laCalc(lintCount).Yield = -1 Then
            '// last
            For lintRun = lintCount To lintMM Step -1
                If laCalc(lintRun).Date > 0 And laCalc(lintRun).Yield <> -1 Then
                    lintLast = lintRun
                    Exit For
                End If
            Next lintRun
            '// next
            For lintRun = lintCount To cMaxMonths
                If laCalc(lintRun).Date > 0 And laCalc(lintRun).Yield <> -1 Then
                    lintNext = lintRun
                    Exit For
                End If
            Next lintRun
        End If
        Do While lintCount < lintNext
            '// interpolation of missing yield
            laCalc(lintCount).Yield = fct_Linear(lintCount, laCalc(lintLast).Yield, laCalc(lintNext).Yield, lintLast, lintNext)
            '// discount factor
            laCalc(lintCount).DCF = (1 - laCalc(lintCount).Yield * ldblSum) / (1 + laCalc(lintCount).Yield * fct_DiffYears(ldblDate, laCalc(lintCount).Date, pstrDayCount))
            ldblSum = ldblSum + laCalc(lintCount).DCF * fct_DiffYears(ldblDate, laCalc(lintCount).Date, pstrDayCount)
            ldblDate = laCalc(lintCount).Date
            lintCount = lintCount + plngComp
        Loop
        '// discount factor for existing yield
        laCalc(lintCount).DCF = (1 - laCalc(lintCount).Yield * ldblSum) / (1 + laCalc(lintCount).Yield * fct_DiffYears(ldblDate, laCalc(lintCount).Date, pstrDayCount))
        ldblSum = ldblSum + laCalc(lintCount).DCF * fct_DiffYears(ldblDate, laCalc(lintCount).Date, pstrDayCount)
        ldblDate = laCalc(lintCount).Date
        lintCount = lintCount + plngComp
    Loop
    
End Sub


Function fct_DiffYears(ByVal pdblFrom As Double, ByVal pdblTo As Double, pstrConv As String) As Double
    
    '// calculate years between pdblFrom and pdblTo according to pstrConv
 
    Select Case pstrConv
        Case "Act/365f"
            fct_DiffYears = (pdblTo - pdblFrom) / 365
        Case "Act/360"
            fct_DiffYears = (pdblTo - pdblFrom) / 360
        Case "Act/Act"
            fct_DiffYears = fct_DateAct(pdblTo) - fct_DateAct(pdblFrom)
        Case "30/360"
            fct_DiffYears = fct_DiffDays30(pdblFrom, pdblTo) / 360
        Case "30E/360"
            fct_DiffYears = fct_DiffDays30E(pdblFrom, pdblTo) / 360
        Case Else
            fct_DiffYears = (pdblTo - pdblFrom) / 365
    End Select
    
End Function

Public Function fct_DateAct(pdblDate As Double) As Double

    '// utility function for Act/Act day count convention
    
    Dim ldblFirstOfThisYear As Double
    Dim ldblFirstOfNextYear As Double
    
    ldblFirstOfThisYear = DateSerial(Year(pdblDate), 1, 1)
    ldblFirstOfNextYear = DateSerial(Year(pdblDate) + 1, 1, 1)
 
    fct_DateAct = Year(pdblDate) + (pdblDate - ldblFirstOfThisYear) / (ldblFirstOfNextYear - ldblFirstOfThisYear)
    
End Function

Function fct_DiffDays30(ByVal pdblFrom As Double, ByVal pdblTo As Double) As Long

    '// calculate days between pdblFrom and pdblTo according to 30/360
 
    Dim lintDays As Integer
    
    lintDays = Day(pdblTo) - IIf(Day(pdblFrom) < 30, Day(pdblFrom), 30) - IIf(Day(pdblFrom) - 29 > 0, IIf(Day(pdblTo) - 30 < 0, 0, Day(pdblTo) - 30), 0)
    fct_DiffDays30 = (Year(pdblTo) - Year(pdblFrom)) * 360 + (Month(pdblTo) - Month(pdblFrom)) * 30 + lintDays
     
End Function

Function fct_DiffDays30E(pdblFrom As Double, pdblTo As Double) As Long

    '// calculate days between pdblFrom and pdblTo according to 30E/360
 
    Dim lintDays As Integer
    
    lintDays = IIf(Day(pdblTo) < 30, Day(pdblTo), 30) - IIf(Day(pdblFrom) < 30, Day(pdblFrom), 30)
    fct_DiffDays30E = (Year(pdblTo) - Year(pdblFrom)) * 360 + (Month(pdblTo) - Month(pdblFrom)) * 30 + lintDays
     
End Function




Public Function fct_Linear(ByVal pdblTime As Double, pdblValueLast As Double, pdblValueNext As Double, ByVal pdblLast As Double, ByVal pdblNext As Double) As Double

    fct_Linear = pdblValueLast + (pdblValueNext - pdblValueLast) * (pdblTime - pdblLast) / (pdblNext - pdblLast)

End Function

Public Function fct_Exponential(ByVal pdblTime As Double, pdblValueLast As Double, pdblValueNext As Double, ByVal pdblLast As Double, ByVal pdblNext As Double) As Double

    Dim ldblLambda As Double
    
    ldblLambda = (pdblNext - pdblTime) / (pdblNext - pdblLast)
    fct_Exponential = Exp((ldblLambda * Log(pdblValueLast) + (1 - ldblLambda) * Log(pdblValueNext)))

End Function




--- Macro File: mdlMain.bas ---
Attribute VB_Name = "mdlMain"

Option Explicit

Public Type typResults
    PeriodCounter As Integer
    Date As Date
    FixCF As Double
    VarCF As Double
    CreditRunOff As Double
End Type
Global dblAct_RORAC As Double
Global dblAct_EbiT As Double
Global dblCurrent_EC As Double
Global strUS_OL As String
Global strRORACTargetCase As String
Global dblPV_Outstanding As Double
Global dateLast_Payment_Date As Date
Global strLast As String
Global dblTarget_Installment As Double


Public Sub prcStartCalculation()

Dim i As Integer
Dim j As Integer
Dim laCurve() As typCurveInput
Dim laSpreads() As typCurveInput
Dim lintRun As Integer
Dim lintInput As Integer
Dim lContract As typContract
Dim arrResults() As typResults
Dim arrNew_CF() As Variant
Dim wksCash_Flow_Module As Worksheet
Dim wksIndex As Worksheet
Dim wksCalculation As Worksheet
Dim dblCalculation_Date As Double
Dim strMM As String
Dim strSW As String
Dim intCompounding_Frequency As Integer
Dim intAnnualized As Integer
Dim arrNew_Credit_Runoff(1 To 121) As Variant

Dim dblSales_Price As Double
Dim dblAdditional_Financed_Items As Double
Dim dblDown_Payment As Double
Dim dblInititial_Direct_Cost As Double
Dim dblSubsidies As Double
Dim dblcontracted_RV As Double
Dim intPayment_Frequency As Integer
Dim strPayment_Mode As String
Dim strInterest_Type As String
Dim datePayout_Date As Date
Dim intRepricing_Term As Integer
Dim intCredit_Term As Integer
Dim intInterest_Only_Period As Integer
Dim arrSkip_Months()
Dim dateExtra_Ordinary_Payment_Date As Date
Dim dblExtra_Ordinary_Payment_Amount As Double
Dim dblNOM_CR As Double
Dim strFinancial_Product_Type As String
Dim dateFirst_Instalment_Date_Input As Date
Dim intInterest_Type_num As Integer
Dim strBuyRateCase As String
Dim strRORACTargetCase As String
Dim dblBuyrate As Double
Dim dblIDC_Adj As Double
Dim wksLGD As Worksheet
Dim intEAD_Adjustment_Factor As Integer
Dim strNew_Used As String
Dim intAge_of_used_Vehicles As Integer
Dim intDisposal_Time As Integer
Dim dblMSRP As Double
Dim dblRemarketing_Cost_Fix As Double
Dim dblRemarketing_Cost_Var As Double
Dim intNumber_Of_Vehicles As Integer
Dim dblAdd_Coll As Double
Dim dblAdd_Coll2 As Double
Dim dblProb_Cure As Double
Dim dblRec_Cure As Double
Dim dblProb_Restr As Double
Dim dblRec_Restr As Double
Dim strDepreciation_Curve As String
Dim strAdd_Coll_Type As String
Dim arrDepreciation_Curve_Table()
Dim arrDCF_Spread_Range_1()
Dim arrPD_Matrix(0 To 119, 0 To 2)
Dim dblTarget_RORAC As Double
Dim dblTarget_EBIT As Double
Dim dblTarget_Customer_Rate As Double
Dim dbl_CTYRisk As Double

Dim dblFinal_PD As Double
Dim strBasel_Type As String
Dim dblManual_LGD As Double
Dim strRV_Balloon As String
Dim dblEC_RVR As Double
Dim dblNAF As Double
Dim dblEC_HC As Double
Dim dblEC_OPR As Double
Dim dblScaling_Factor As Double
Dim dblHurdle_Rate As Double
Dim dblFundingR As Double
Dim dblSpread As Double
Dim dblNIBL As Double
Dim dblRV_Enhancements As Double
Dim dblDeal_Rate As Double
Dim dblManual_MFR_Interest As Double
Dim dblManual_MFR_Spread As Double
Dim intcounter As Integer
Dim intCounterRORAC As Integer
Dim dblLast_Instalment As String
Dim dblStandardCost_RVR As Double
Dim dblOutstanding_Interest_Binding_Pre_Period As Double
Dim dateCash_Flow_Date_Pre_Period As Date
Dim dateCash_Flow_Date_Current_Period As Date
Dim strFlag As String
Dim dblAmmortization_Method As Double
Dim intBumpUp_Case As Integer
Dim dblLast_NPV As Double
Dim dblLast_NOM_CR As Double
Dim dblCurr_NOM_CR As Double
Dim dblFinal_PD2 As Double


start:


dblAct_RORAC = 0
dblAct_EbiT = 0
dblCurrent_EC = 0
strLast = ""
strRORACTargetCase = [Target_RORAC_Case]
Set wksIndex = Sheets("Index")
dateLast_Payment_Date = 0

If strRORACTargetCase = "No" And strLast = "" Then

wksIndex.Range("Mdl_Credit_Runoff").ClearContents
wksIndex.Range("Mdl_MFR").ClearContents
wksIndex.Range("Mdl_MFS").ClearContents
wksIndex.Range("Mdl_Final_CF").ClearContents
wksIndex.Range("Mdl_LGD_Total").ClearContents
wksIndex.Range("Mdl_Funding_Adjustment").ClearContents
wksIndex.Range("Mdl_NIBL_Adjustment").ClearContents
wksIndex.Range("Mdl_CoCR").ClearContents
wksIndex.Range("Mdl_CoRVR").ClearContents
wksIndex.Range("Mdl_EC_Credit_Risk").ClearContents
wksIndex.Range("Mdl_EC_RV_Risk").ClearContents
wksIndex.Range("Mdl_EC_HC_Risk").ClearContents
wksIndex.Range("Mdl_EC_Operational").ClearContents
wksIndex.Range("Mdl_EC").ClearContents
wksIndex.Range("Mdl_Cost_of_Equity").ClearContents
wksIndex.Range("Mdl_EAD").ClearContents
wksIndex.Range("Mdl_Colleteral_Value").ClearContents
wksIndex.Range("Mdl_LGD_Liqui").ClearContents
wksIndex.Range("Mdl_Collateral_Value").ClearContents
wksIndex.Range("Mdl_EC_Country_Risk").ClearContents


    If [write_mcf] = "yes" Then
        Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2").ClearContents
        Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_3").ClearContents
        Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_4").ClearContents
    End If

End If

dblSales_Price = wksIndex.Range("Sales_Price")
dblAdditional_Financed_Items = wksIndex.Range("Additional_Financed_Items")
dblDown_Payment = wksIndex.Range("DOWN_P")
dblInititial_Direct_Cost = wksIndex.Range("IDC")
dblSubsidies = wksIndex.Range("Subsidies")
dblcontracted_RV = wksIndex.Range("Cont_RV")
intPayment_Frequency = wksIndex.Range("Payment_Frequency")
strPayment_Mode = wksIndex.Range("Payment_Mode")
strInterest_Type = wksIndex.Range("Interest_Type")
datePayout_Date = wksIndex.Range("Payout_Date")
intCredit_Term = wksIndex.Range("Maturity")
intRepricing_Term = wksIndex.Range("Repricing_Term")
intInterest_Only_Period = wksIndex.Range("Interest_Only_Period")
dateFirst_Instalment_Date_Input = wksIndex.Range("First_Instalment_Input")
'dateExtra_Ordinary_Payment_Date = wksIndex.Range("EO_Payment_Date")
'dblExtra_Ordinary_Payment_Amount = wksIndex.Range("EO_Amount")
dblNOM_CR = wksIndex.Range("NOM_CR") / 100
strFinancial_Product_Type = wksIndex.Range("Financial_Product_Type")
dblLast_Instalment = wksIndex.Range("Last_Instalment")

ReDim arrSkip_Months(0 To wksIndex.Range("Skip_Months").Rows.Count - 1)
For i = LBound(arrSkip_Months()) To UBound(arrSkip_Months())
    arrSkip_Months(i) = wksIndex.Range("Skip_Months")(i + 1)
Next i
 
strUS_OL = wksIndex.Range("US_OL").value

'For postpone balloon case the credit term is extended by one payment period. In some entities (e.g. USA, Portugal) the customer return the car
'not at the end of the contract but one payment period (usually a month) later. Actually it can be assumed that for most of the leasing contracts
'the return of the car is not exactly at the last day of the leasing contract. The general assumption that for a leasing contract the last payment
'(comparable to a ballon payment with a financing contract) is received at the end of the contract is questionable but however, currently used in
'the RORAC calculation and, by the way, also for refinancing purposes (ie ALM).

If strUS_OL = "Yes" Then
    intCredit_Term = intCredit_Term + intPayment_Frequency
End If
            

intInterest_Type_num = [Interest_Type_num]

ReDim arrDepreciation_Curve_Table(0 To wksIndex.Range("Depreciation_Curve_Table_der").Rows.Count - 1, _
                                  0 To wksIndex.Range("Depreciation_Curve_Table_der").Columns.Count - 1)

For i = 0 To wksIndex.Range("Depreciation_Curve_Table_der").Rows.Count - 1
    For j = 0 To wksIndex.Range("Depreciation_Curve_Table_der").Columns.Count - 1
        arrDepreciation_Curve_Table(i, j) = wksIndex.Range("Depreciation_Curve_Table_der")(i + 1, j + 1)
    Next j
Next i


dateFirst_Instalment_Date_Input = wksIndex.Range("First_Instalment_Input")
intEAD_Adjustment_Factor = wksIndex.Range("EAD_Adjustment_Factor")
intAge_of_used_Vehicles = wksIndex.Range("Age_of_used_Vehicles")
intDisposal_Time = wksIndex.Range("Disposal_Time")
dblMSRP = wksIndex.Range("MSRP")
dblRemarketing_Cost_Fix = wksIndex.Range("Remarketing_Cost_Fix")
dblRemarketing_Cost_Var = wksIndex.Range("Remarketing_Cost_Var")
intNumber_Of_Vehicles = wksIndex.Range("Number_of_vehicles")
strAdd_Coll_Type = wksIndex.Range("Add_Coll_Type")
'dblAdd_Coll = wksIndex.Range("Add_Coll")
dblAdd_Coll2 = wksIndex.Range("Add_Coll_2")
dblProb_Cure = wksIndex.Range("Prob_Cure")
dblRec_Cure = wksIndex.Range("Rec_Cure")
dblProb_Restr = wksIndex.Range("Prob_Restr")
dblRec_Restr = wksIndex.Range("Rec_Restr")
strDepreciation_Curve = wksIndex.Range("Depreciation_Curve")
                                             
If intAge_of_used_Vehicles > 0 And [Country_Short] <> "USA" And [Country_Short] <> "CAN" Then
    strNew_Used = "Used"
Else
    strNew_Used = "New"
End If

dblCalculation_Date = wksIndex.Range("Calculation_Date")
strMM = wksIndex.Range("Day_Convention_MM")
strSW = wksIndex.Range("Day_Convention_SM")
intCompounding_Frequency = wksIndex.Range("Compounding_Frequency")
intAnnualized = wksIndex.Range("Annualized")
strBasel_Type = wksIndex.Range("Basel_Type")
dblManual_LGD = wksIndex.Range("Manual_LGD") / 100
strRV_Balloon = wksIndex.Range("RV_Balloon")
dblEC_RVR = wksIndex.Range("EC_RVR")
dblNAF = wksIndex.Range("NAF")
dblEC_HC = wksIndex.Range("EC_HC")
dblEC_OPR = wksIndex.Range("EC_OPR")
dblScaling_Factor = wksIndex.Range("Scaling_Factor")
dblHurdle_Rate = wksIndex.Range("Hurdle_Rate")
dblNIBL = wksIndex.Range("NIBL")
dblRV_Enhancements = wksIndex.Range("RV_Enhancements")
dblManual_MFR_Interest = wksIndex.Range("Manual_MFR_Interest")
dblManual_MFR_Spread = wksIndex.Range("Manual_MFR_Spread")
dblFinal_PD = wksIndex.Range("Final_PD")
dblFinal_PD2 = wksIndex.Range("Final_PD2")
dbl_CTYRisk = wksIndex.Range("Country_Risk")


For i = LBound(arrPD_Matrix()) To UBound(arrPD_Matrix())
    arrPD_Matrix(i, 0) = i + 1
    
    If i = 0 Then
        arrPD_Matrix(i, 1) = dblFinal_PD / 100
    ElseIf i < 12 Then
        arrPD_Matrix(i, 1) = arrPD_Matrix(0, 1)
    ElseIf i < 24 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 1 * arrPD_Matrix(0, 1)
    ElseIf i < 36 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 2 * arrPD_Matrix(0, 1)
    ElseIf i < 48 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 3 * arrPD_Matrix(0, 1)
    ElseIf i < 60 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 4 * arrPD_Matrix(0, 1)
    ElseIf i < 72 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 5 * arrPD_Matrix(0, 1)
    ElseIf i < 84 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 6 * arrPD_Matrix(0, 1)
    ElseIf i < 96 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 7 * arrPD_Matrix(0, 1)
    ElseIf i < 108 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 8 * arrPD_Matrix(0, 1)
    ElseIf i < 120 Then
        arrPD_Matrix(i, 1) = (1 - arrPD_Matrix(0, 1)) ^ 9 * arrPD_Matrix(0, 1)
    End If
    
    arrPD_Matrix(i, 2) = (1 - (1 - arrPD_Matrix(i, 1) / 100) ^ (intPayment_Frequency / 12)) * 100
Next i



'// read yield curve
lintInput = 0
    For lintRun = 1 To wksIndex.Range("Interest_Spread_Curve").Rows.Count
        If wksIndex.Range("Interest_Spread_Curve")(lintRun, 2).value <> "" Then
            ReDim Preserve laCurve(lintInput)
            laCurve(lintInput).Type = wksIndex.Range("Interest_Spread_Curve")(lintRun, 1).value
            laCurve(lintInput).Yield = wksIndex.Range("Interest_Spread_Curve")(lintRun, 2).value
            lintInput = lintInput + 1
        End If
    Next lintRun

'// read spread curve
lintInput = 0
    For lintRun = 1 To wksIndex.Range("Interest_Spread_Curve").Rows.Count
        If wksIndex.Range("Interest_Spread_Curve")(lintRun, 3).value <> "" Then
            ReDim Preserve laSpreads(lintInput)
            laSpreads(lintInput).Type = wksIndex.Range("Interest_Spread_Curve")(lintRun, 1).value
            laSpreads(lintInput).Yield = wksIndex.Range("Interest_Spread_Curve")(lintRun, 3).value
            lintInput = lintInput + 1
        End If
    Next lintRun
    
'Target RORAC calculation
'Depending on the target rate type differet variables are set at start values (for case 1, 2 and 4 the nominal customer rate is set and for case 3 the subsidy variable is set)

If [Target_RORAC_Case] = "Yes" Then
    Select Case [Target_Type]
    Case 1, 2, 4
    dblNOM_CR = [Start_RORAC]
    strLast = "No"
    Case 3
    dblSubsidies = [Start_RORAC]
    strLast = "No"
    End Select
Else
GoTo NonRORACTargetCase
End If

     
'Depending on the target rate type differet variables are set at targeted end values (for case 1 and 3 the RORAC rate is set, for case 2 the EbIT variable is set, for case 2 the target Installment is set)

Select Case [Target_Type]
Case 1
dblTarget_RORAC = Worksheets("Index").Range("Target_RORAC").value
Case 2
dblTarget_EBIT = Worksheets("Index").Range("Target_EBIT").value
Case 3
dblTarget_RORAC = Worksheets("Index").Range("Target_RORAC").value
Case 4
dblTarget_Installment = Worksheets("Index").Range("Target_Instalment").value
End Select

RORACTargetCase:

'After each iteration process actual and target value of the relevant variable is compared. As long as the difference lies above or equals a given hurdle rate
'the intedended variable (e.g. dblNOM_CR in case 1 and 2) is adjusted and the next iteration process is started. As soon as the difference falls below a given hurdle the iteration process
'is stopped and the final values are written into the output variables (e.g. [Target_Rate]) on the Index sheet.

Select Case [Target_Type]
Case 1
If Abs(dblTarget_RORAC - dblAct_RORAC) < 0.00001 Then
strRORACTargetCase = "No"
  If [Buy_Rate_Case] = "Yes" Then
  [Target_Rate] = lContract.IRR
  Else
    'For Mexico in some cases the final customer interest rate contains tax and hence the tax has to be added on top of the initial customer rate excl. tax
    If [Country_Short] = "MEX" And [tax_rate] > 0 And [TaxRefBase] = "Interest" And [TaxQuotationType] = 1 Then
        If [IDC] - dblSubsidies <> 0 Then
        [Target_Rate] = (dblNOM_CR / (1 + [tax_rate]))
        Else
        [Target_Rate] = (lContract.IRR * (1 + [tax_rate]))
        End If
    Else
    [Target_Rate] = dblNOM_CR
    End If
  End If
Else
  If intCounterRORAC > 0 Then
  dblNOM_CR = dblNOM_CR + ((dblTarget_RORAC * dblCurrent_EC) - dblAct_EbiT)
     If Worksheets("New Input Mask").Range("H41").value <> 0 Then
     Worksheets("New Input Mask").Range("H54").value = dblNOM_CR * 100
     Application.Calculate
     End If
  End If
End If
Case 2
    If Abs(dblTarget_EBIT - (dblAct_EbiT * dblPV_Outstanding)) < 0.001 Then
    strRORACTargetCase = "No"
        If [Buy_Rate_Case] = "Yes" Then
        [Target_Rate] = lContract.IRR
        Else
        [Target_Rate] = dblNOM_CR
        End If
    Else
        If intCounterRORAC > 0 Then
        dblNOM_CR = dblNOM_CR + ((dblTarget_EBIT - (dblAct_EbiT * dblPV_Outstanding)) / dblPV_Outstanding)
            If Worksheets("New Input Mask").Range("H41").value <> 0 Then
            Worksheets("New Input Mask").Range("H54").value = dblNOM_CR * 100
            Application.Calculate
            End If
        End If
    End If
Case 3
    If Abs(dblTarget_RORAC - dblAct_RORAC) < 0.00001 Then
    strRORACTargetCase = "No"
    [Target_Rate] = dblSubsidies
    Else
        If intCounterRORAC > 0 Then
        dblSubsidies = dblSubsidies + (((dblTarget_RORAC * dblCurrent_EC) - dblAct_EbiT) * dblPV_Outstanding)
            If Worksheets("New Input Mask").Range("H41").value <> 0 Then
            Worksheets("New Input Mask").Range("H54").value = dblNOM_CR * 100
            Application.Calculate
            End If
        End If
    End If
Case 4
    If Abs(dblTarget_Installment - dblAct_Installment) < 0.001 Then
    strRORACTargetCase = "No"
        If [Buy_Rate_Case] = "Yes" Then
        [Target_Rate] = lContract.IRR
        Else
        [Target_Rate] = dblNOM_CR
        End If
    Else
        If [Interest_Type] <> "Fix" And [amortization_method] = 1 Then
            If intCounterRORAC > 0 Then
            Debug.Print intCounterRORAC
            dblNOM_CR = (dblTarget_Installment - arrCF(1, 2)) / (-arrCF(0, 2) * fct_DiffDays30(arrCF(0, 1), arrCF(1, 1)) / 360)

            If Worksheets("New Input Mask").Range("H41").value <> 0 Then
            Worksheets("New Input Mask").Range("H54").value = dblNOM_CR * 100
            Application.Calculate
            End If
            End If
        Else
            If intCounterRORAC = 1 Then
                dblCurr_NOM_CR = dblNOM_CR
                dblNOM_CR = dblCurr_NOM_CR - dblNPV / ((dblNPV - dblIni_NPV) / (dblCurr_NOM_CR - 0))
                dblLast_NPV = dblNPV

                If Worksheets("New Input Mask").Range("H41").value <> 0 Then
                    Worksheets("New Input Mask").Range("H54").value = dblNOM_CR * 100
                    Application.Calculate
                End If
            ElseIf intCounterRORAC > 1 Then
                dblLast_NOM_CR = dblCurr_NOM_CR
                dblCurr_NOM_CR = dblNOM_CR
                dblNOM_CR = dblCurr_NOM_CR - dblNPV / ((dblNPV - dblLast_NPV) / (dblCurr_NOM_CR - dblLast_NOM_CR))
                dblLast_NPV = dblNPV
                
                If Worksheets("New Input Mask").Range("H41").value <> 0 Then
                    Worksheets("New Input Mask").Range("H54").value = dblNOM_CR * 100
                    Application.Calculate
                End If
            End If
        End If
    End If
End Select
NonRORACTargetCase:

' Buy Rate Case
'For buy rate cases the nominal customer rate entered on the Input Mask becomes the target Deal Rate and within an iteration process the program searches for a new nominal rate
'which, under consideration of an IDC effect based on the IDC entered on the Input Mask, yields a Deal Rate which is equal to the targeted deal rate. For example, if a 5%
'customer rate and 100 Euro IDCs (which become for example 0.5% periodic IDC margin) are entered, the program searches for a new nominal customer rate for which the following relationship holds true:
'new nominal customer rate - 0.5% IDC margin = 5% (Deal Rate)

If [Buy_Rate_Case] = "Yes" And [Target_RORAC_Case] = "No" Then
    'NON BumpRate case for standard calculation
    If (Worksheets("Index").Range("IDC").value + Worksheets("Index").Range("Subsidies").value) <> 0 Then
        dblBuyrate = [Nom_CR] / 100
        dblIDC_Adj = (Worksheets("Index").Range("IDC").value / (intCredit_Term / 12)) / ((Worksheets("Index").Range("NAF").value + Worksheets("Index").Range("Cont_RV").value) / 2)
        dblNOM_CR = dblBuyrate + dblIDC_Adj
        strBuyRateCase = "Yes"
    Else
        If Worksheets("Index").Range("Country_Short") = "USA" Then
        'BumpRate case for USA
            dblNOM_CR = ([Nom_CR] / 100) + Worksheets("Index").Range("Rate_Bump").value
            dblInititial_Direct_Cost = Worksheets("Index").Range("Rate_Bump").value * (((Worksheets("Index").Range("NAF").value + Worksheets("Index").Range("Cont_RV").value) / 2) / (intCredit_Term / 12))
            intBumpUp_Case = 1
            strBuyRateCase = "Yes"
        End If
     GoTo NonBuyRateCase
     End If
End If


'++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
BuyRateCase:
If intBumpUp_Case = 0 Then
    'NON BumpRate case for standard calculation
    If Abs(lContract.IRR - dblBuyrate) < 0.0000001 Then
        strBuyRateCase = "No"
    Else
        If intcounter > 0 Then
            dblNOM_CR = dblNOM_CR + (dblBuyrate - lContract.IRR) / 2
        End If
    End If
Else
    'BumpRate case for USA
    If Abs(dblNOM_CR - lContract.IRR - Worksheets("Index").Range("Rate_Bump").value) < 0.000001 Then
        strBuyRateCase = "No"
        Worksheets("New Input Mask").Range("E37").value = dblInititial_Direct_Cost
    Else
        If intcounter > 0 Then
            dblInititial_Direct_Cost = dblInititial_Direct_Cost - ((dblNOM_CR - lContract.IRR - Worksheets("Index").Range("Rate_Bump").value) * ((Worksheets("Index").Range("NAF").value + Worksheets("Index").Range("Cont_RV").value) / (intCredit_Term / 12))) / 2
        End If
    End If
End If

NonBuyRateCase:

Dim counter As Integer
counter = 0

'// Start of Calculation
arrCF = fctCash_Flow_Generation(intPayment_Frequency, _
                        strInterest_Type, _
                        intCredit_Term, _
                        intInterest_Only_Period, _
                        datePayout_Date, _
                        dblNOM_CR, _
                        dblNAF, _
                        dblSales_Price, _
                        dblAdditional_Financed_Items, _
                        dblDown_Payment, _
                        dblcontracted_RV, _
                        dblInititial_Direct_Cost, _
                        dblSubsidies, _
                        strPayment_Mode, _
                        strUS_OL, _
                        dateExtra_Ordinary_Payment_Date, _
                        dblExtra_Ordinary_Payment_Amount, _
                        dateFirst_Instalment_Date_Input, _
                        arrSkip_Months(), _
                        intInterest_Type_num, _
                        dblLast_Instalment, _
                        strRORACTargetCase, _
                        lContract, _
                        arrCF, laCurve, laSpreads, dblCalculation_Date, strMM, strSW, intCompounding_Frequency, intAnnualized)
                        
'For i = 0 To UBound(arrCF())
'    Debug.Print arrCF(i, 0); arrCF(i, 1); arrCF(i, 2); arrCF(i, 3)
'Next i

ReDim arrResults(0 To UBound(arrCF))
ReDim arrNew_CF(0 To UBound(arrCF), 1 To 3)

dblAdd_Coll = wksIndex.Range("Add_Coll")

Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, strMM, _
strSW, intCompounding_Frequency, intAnnualized)


'For i = 0 To UBound(arrCF())
'    Debug.Print lContract.LiqRunoff(i + 1).NBV
'Next i

If strBuyRateCase = "Yes" Then
    intcounter = intcounter + 1
    GoTo BuyRateCase
End If

'++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
'new fixed cash flow for fixed rate IDC/Subsidy contracts

If [IDC] - dblSubsidies <> 0 And [Interest_Type] = "Fix" Then
    For i = 0 To UBound(arrCF)
        If i <= [Interest_Only_Period] And i > 0 Then
            arrCF(i, 2) = [NAF] * dblNOM_CR * fct_DiffDays30(arrCF(i - 1, 1), arrCF(i, 1)) / 360
        End If
    Next i

    Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, _
    strMM, strSW, intCompounding_Frequency, intAnnualized)

End If

If [write_mcf] = "yes" Then
    For i = 0 To intArray_Limit / intPayment_Frequency
        j = 0
        Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, j + 1) = arrCash_Flow_Generation(i, 0)
        Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_4")(i + 1, j + 1) = "No"
    Next i
    
    
    For i = 0 To intArray_Limit / intPayment_Frequency
        For j = 1 To 2
            Select Case j
            Case 1
            Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, j + 1) = arrCash_Flow_Generation(i, 4)
            Case 2
            If arrCash_Flow_Generation(i, 8) = 1 Then
               Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, j + 2) = "Principal and Interest"
            Else
                Worksheets("Manual_Cash_Flows").Range("Mdl_Final_CF_2")(i + 1, j + 2) = "No Regular Payment"
            End If
            End Select
         Next j
    Next i
End If

'--Cash Flow Results to Excel------------------------------------------------

'NBV runoff for IDC/subsidy contract
'The customer outstand does not contain IDC or subsidies. For example, if we pay a commission to a Dealer the customer oustanding is not increased,
'i.e. the customer does not owe us more money. It is only via the interest payments that the customer pays for the commission and hence the customer rate will
'increase. In order to have the correct customer runoff, which could be shown to the customer, the IDC and subsdidies which are part of the current cash
'flow need to be eliminated again (arrCF(0, 2) = arrCF(0, 2) + [IDC] - dblSubsidies) and a new runoff needs to be calculated and stored a separate array (arrNew_Credit_Runoff(i))
'which is used in other routines in the mdlMain module (search for "arrNew_Credit_Runoff(i)"). After that the old cashflow (incl. IDC and subsidies) is created again 8arrCF(0, 2) = arrCF(0, 2) - [IDC] + dblSubsidies).

If [IDC] - dblSubsidies <> 0 Then
    arrCF(0, 2) = arrCF(0, 2) + [IDC] - dblSubsidies
    Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, _
    strMM, strSW, intCompounding_Frequency, intAnnualized)
    
    For i = 1 To UBound(arrCF)
    arrNew_Credit_Runoff(i) = lContract.LiqRunoff(i).NBV
    Next i
    
    arrCF(0, 2) = arrCF(0, 2) - [IDC] + dblSubsidies
    Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, _
    strMM, strSW, intCompounding_Frequency, intAnnualized)
End If


'new cash flow and calculation for variable contracts with annuity amortization
'For variable contracts with annuity amortization the principal payments have to be calculated based on a fixed customer rate. The assumption
'for this calculation will be that the entered customer rate (ie the initial rate which gets changed with the next interest rate fixing) is fixed over
'the total life time of the contract.

If [Interest_Type] <> "Fix" And [amortization_method] = 2 Then

    For i = 0 To UBound(arrCF)
    arrResults(i).FixCF = arrCF(i, 2)
    Next i
 

' write new amortization schedule based on credit runoff into new cash flow
    
    For i = 0 To UBound(arrNew_CF)
        'cash flow dates do not change
        arrNew_CF(i, 1) = arrCF(i, 1)
        If i = 0 Then
            'the first cash flow equals the negative outstanding at contract start
            arrNew_CF(i, 2) = -lContract.LiqRunoff(i + 1).NBV
        Else
            If arrResults(i).PeriodCounter > intCredit_Term Then
                arrNew_CF(i, 2) = 0
            Else
                If [IDC] - dblSubsidies <> 0 Then
                'For contracts with IDC and/or subsdidies the NBV from the cash flow excl. IDCs and subsidies (arrNew_Credit_Runoff) is used to calculate the principal amounts. This is done by
                'calculating the difference between the NBV of the current cash flow date and the next cash flow date (i+1)
                arrNew_CF(i, 2) = arrNew_Credit_Runoff(i) - arrNew_Credit_Runoff(i + 1)
                Else
                'For contracts without IDC or subsdidies the NBV from the original cash flow is used to calculate the principal amounts. This is done by
                'calculating the difference between the NBV of the current cash flow date and the next cash flow date (i+1)
                arrNew_CF(i, 2) = lContract.LiqRunoff(i).NBV - lContract.LiqRunoff(i + 1).NBV
                End If
            End If
        End If
    
    
    ' calculate and write variable cash flows into new cash flow
    'Beside the principal cashflows (which are in column 2 of the arrNew_CF array) the interest cashflows for the initial interest fixing period
    '(depends on the interest rate type; e.g. 1-month-variable = 1-month fixing period) has to be calculated and written into column 3 of the
    'arrNew_CF array. It is done by:
    'a) for contracts with IDC and/or subsidies by using the NBV from the cash flow excl. IDCs and subsidies (arrNew_Credit_Runoff) and calculating
    'the interests for the respective period by using nominal customer rate and start/end date of the period.
    'b)For contracts without IDC or subsdidies the difference between the customer instalment (contains principal and interest portion) from the
    'original cash flow  which was written into arrResults(i).FixCF and the principal portion which was calculated in the step before (arrNew_CF(i, 2)).
    
    If i = 0 Then
        arrNew_CF(i, 3) = 0
    Else
        If arrCash_Flow_Generation(i - 1, 17) = 0 Then
        arrNew_CF(i, 3) = 0
        Else
            If [IDC] - dblSubsidies <> 0 Then
            arrNew_CF(i, 3) = arrNew_Credit_Runoff(i) * dblNOM_CR * fct_DiffDays30(arrCF(i - 1, 1), arrCF(i, 1)) / 360
            Else
            arrNew_CF(i, 3) = arrResults(i).FixCF - arrNew_CF(i, 2)
            End If
        End If
    End If
    Next i
        
    'write new cash flow into old cash flow array
    
    For i = 0 To UBound(arrNew_CF)
    For j = 1 To 3
        arrCF(i, j) = arrNew_CF(i, j)
    Next j
    Next i

'For i = 0 To UBound(arrCF())
'    Debug.Print arrCF(i, 1); arrCF(i, 2); arrCF(i, 3)
'Next i

       
'Calculate Credit_Runoff with new Cash_Flow
    Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, _
    strMM, strSW, intCompounding_Frequency, intAnnualized)
End If
'end new cash flow and calculation for variable contracts with annuity amortization


'new credit runoff for contracts with Zero interest periods (principal only in manual cash flow sheet)
'For principal only period the interest rate which is used to calculate the discount factor for the calculation of instalment
'payments is set to 0 in the cashflow module. By this overall IRR of the contract will fall below nominal interest rate and reflect
'periods where the total instalment is handled as principal payments (ie no interest payment = interest rate equals Zero).
'Now here the runoff is corrected by calculating the interest payments based on the nomincal rate for all regular payment periods and
'considering the total instalment as principal payments for the principal only periods.

If [Manual_CF_Flag] = 1 And [Principal_Only_Count] > 0 Then
For i = 1 To UBound(arrCF)
    If Worksheets("Manual_Cash_Flows").Cells(i + 2, 5) = "Principal Only" Then
        lContract.LiqRunoff(i + 1).NBV = lContract.LiqRunoff(i).NBV - arrCF(i, 2)
    Else
        lContract.LiqRunoff(i + 1).NBV = lContract.LiqRunoff(i).NBV + (lContract.LiqRunoff(i).NBV * dblNOM_CR * fct_DiffDays30(arrCF(i - 1, 1), arrCF(i, 1)) / 360) - arrCF(i, 2)
    End If
Next i
End If

'create new cash flow for Mexico when tax payments are based on interest and total customer payment is constant

If [Country_Short] = "MEX" And [tax_rate] > 0 And [TaxRefBase] = "Interest" And [TaxQuotationType] = 1 Then
    For i = 1 To UBound(arrCF)
        If [IDC] - dblSubsidies <> 0 Then
            arrCF(i, 2) = arrCF(i, 2) - (arrNew_Credit_Runoff(i) * (dblNOM_CR / (1 + [tax_rate])) * [tax_rate]) * fct_DiffDays30(arrCF(i - 1, 1), arrCF(i, 1)) / 360
        Else
            arrCF(i, 2) = arrCF(i, 2) - (lContract.LiqRunoff(i).NBV * (dblNOM_CR / (1 + [tax_rate])) * [tax_rate]) * fct_DiffDays30(arrCF(i - 1, 1), arrCF(i, 1)) / 360
        End If
    Next i
    Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, strMM, _
    strSW, intCompounding_Frequency, intAnnualized)
End If


'--Cash Flow Results to Excel------------------------------------------------
If strRORACTargetCase = "No" And strLast = "" Then
    For i = 0 To UBound(arrCF)
        For j = 1 To 3
            wksIndex.Range("Mdl_Final_CF")(i + 1, j) = arrCF(i, j)
        Next j
    Next i
End If

'contract runoff results To Excel

If strRORACTargetCase = "No" And strLast = "" Then

For lintRun = LBound(lContract.LiqRunoff) To UBound(lContract.LiqRunoff)
    wksIndex.Range("Mdl_Credit_Runoff")(lintRun, 1) = CVDate(lContract.LiqRunoff(lintRun).Date)
    wksIndex.Range("Mdl_Credit_Runoff")(lintRun, 2) = lContract.LiqRunoff(lintRun).NBV
Next lintRun

'bug for variable contract+buyrate, commented secion marked with arrows
'->
'wksIndex.Range("Mdl_NOM_CR") = dblNOM_CR
'wksIndex.Range("Mdl_IRR") = lContract.IRR
'wksIndex.Range("Mdl_MFR") = lContract.MFR
'|<-

''new: commented on 3.12.2013 bank branch new code below
'wksIndex.Range("Mdl_NOM_CR") = dblNOM_CR
'wksIndex.Range("Mdl_MFR") = lContract.MFR

wksIndex.Range("Mdl_NOM_CR") = dblNOM_CR
If wksIndex.Range("Is_Bank_Branch_deal") And wksIndex.Range("Borrower_Type").value = "Corporate Dealer" Then
    Select Case wksIndex.Range("Deal_Currency").value
        Case Worksheets("I_and_S").Range("C2").value
            wksIndex.Range("Mdl_MFR") = Worksheets("I_and_S").Range("Manual_MFR1").value / 100
            lContract.MFR = Worksheets("I_and_S").Range("Manual_MFR1").value / 100
        Case Worksheets("I_and_S").Range("I2").value
            wksIndex.Range("Mdl_MFR") = Worksheets("I_and_S").Range("Manual_MFR2").value / 100
            lContract.MFR = Worksheets("I_and_S").Range("Manual_MFR2").value / 100
        Case Worksheets("I_and_S").Range("O2").value
            wksIndex.Range("Mdl_MFR") = Worksheets("I_and_S").Range("Manual_MFR3").value / 100
            lContract.MFR = Worksheets("I_and_S").Range("Manual_MFR3").value / 100
    End Select
Else
    wksIndex.Range("Mdl_MFR") = lContract.MFR
End If

'no overwrite of IRR for buyrate case contracts

If [Buy_Rate_Case] = "Yes" And [Target_RORAC_Case] = "No" Then
    wksIndex.Range("Mdl_IRR") = dblBuyrate
Else
    wksIndex.Range("Mdl_IRR") = lContract.IRR
End If
'no overwrite of MFS for contracts with repricing term < credit term

If strUS_OL = "Yes" Then
    If intRepricing_Term = intCredit_Term - intPayment_Frequency Then
    wksIndex.Range("Mdl_MFS") = lContract.MFS
    End If
End If

If strUS_OL = "No" Then
        If intRepricing_Term = intCredit_Term Then
        If wksIndex.Range("Is_Bank_Branch_deal") And wksIndex.Range("Borrower_Type").value = "Corporate Dealer" Then
            wksIndex.Range("Mdl_MFS") = 0
            lContract.MFS = 0
        Else
            wksIndex.Range("Mdl_MFS") = lContract.MFS
        End If
    End If
End If

wksIndex.Range("Mdl_Effective_Maturity") = dblEffective_Maturity

End If



arrLGD = fctLGD_Generation(datePayout_Date, _
                intPayment_Frequency, _
                dateFirst_Instalment_Date_Input, _
                arrSkip_Months(), _
                intEAD_Adjustment_Factor, _
                dblInititial_Direct_Cost, _
                dblSubsidies, _
                intCredit_Term, _
                lContract.LiqRunoff, _
                strNew_Used, _
                strDepreciation_Curve, _
                arrDepreciation_Curve_Table(), _
                intAge_of_used_Vehicles, _
                dblMSRP, _
                intDisposal_Time, _
                dblRemarketing_Cost_Fix, _
                intNumber_Of_Vehicles, _
                dblRemarketing_Cost_Var, _
                strAdd_Coll_Type, _
                dblAdd_Coll, dblAdd_Coll2, _
                dblProb_Cure, _
                dblRec_Cure, _
                dblProb_Restr, _
                dblRec_Restr, _
                strUS_OL, dblSales_Price, dblManual_LGD, lContract, arrNew_Credit_Runoff(), intRepricing_Term, arrResults())
                
                




'--LGD Results to Excel Sheet Index------------------------------------------------
If strRORACTargetCase = "No" And strLast = "" Then
For i = 0 To intArray_Limit
    wksIndex.Range("Mdl_LGD_Total")(i + 1) = arrLGD(i, 3)
    wksIndex.Range("Mdl_EAD")(i + 1) = arrLGD(i, 0)
    wksIndex.Range("Mdl_Colleteral_Value")(i + 1) = arrLGD(i, 1)
    wksIndex.Range("Mdl_LGD_Liqui")(i + 1) = arrLGD(i, 2)
    wksIndex.Range("Mdl_Collateral_Value")(i + 1) = arrLGD_Generation(i, 7)

Next i

End If



'--LGD Results to Excel Index-----------------------------------------------

'--LGD Results to Excel Sheet LGD------------------------------------------------
'Call fctClear_Range(Sheets("LGD").Range("Mdl_LGD"))
'If strRORACTargetCase = "No" And strLast = "" Then
'For i = 0 To UBound(arrCF)
 '   For j = 1 To 18
 '   Sheets("LGD").Range("Mdl_LGD")(i + 1, j) = arrLGD_Generation(i, j - 1)
'Next j
'Next i
'End If

'--LGD Results to Excel Sheet LGD------------------------------------------------


    

dblDeal_Rate = lContract.IRR
dblFundingR = lContract.MFR

dblSpread = lContract.MFS

'create new cash flow for repricing term < credit term
'For variable rate contracts where the repricing term (term for the setting of the customer spread) is smaller than the credit term (maturity of the contract)
'the matched funded rate spreads (MFS) has to be re-calculated based on the real repricing term. For this a new cash flow is needed where the total outstanding
'is paid back after the end of the first repricing period. After the new MFS is calculated and written into the Index sheet (mdl_MFS) the old cash flow is used again
'to calculate the original runoffs.

If intRepricing_Term < intCredit_Term And [Interest_Type] <> "Fix" Then
    'create new cash flow
    For i = 0 To UBound(arrCF)
    arrResults(i).PeriodCounter = arrCF(i, 0)
    arrResults(i).Date = arrCF(i, 1)
    arrResults(i).FixCF = arrCF(i, 2)
    arrResults(i).VarCF = arrCF(i, 3)
    arrResults(i).CreditRunOff = lContract.LiqRunoff(i + 1).NBV
    Next i

    For i = 0 To UBound(arrNew_CF)
        arrNew_CF(i, 1) = arrResults(i).Date
        arrNew_CF(i, 3) = arrResults(i).VarCF
        If i = 0 Then
            arrNew_CF(i, 2) = -arrResults(i).CreditRunOff
        Else
            If arrResults(i).PeriodCounter > intRepricing_Term Then
                arrNew_CF(i, 2) = 0
            Else
                If arrResults(i).PeriodCounter = intRepricing_Term Then
                    arrNew_CF(i, 2) = arrResults(i - 1).CreditRunOff
                Else
                    arrNew_CF(i, 2) = arrResults(i - 1).CreditRunOff - arrResults(i).CreditRunOff
                End If
            End If
        End If
    Next i
    
    
    'Calculate Credit_Runoff with new Cash_Flow
    Call sub_CalcCF(lContract, arrNew_CF, laCurve, laSpreads, dblCalculation_Date, _
    strMM, strSW, intCompounding_Frequency, intAnnualized)



    'Results To Excel
    If strRORACTargetCase = "No" And strLast = "" Then

    wksIndex.Range("Mdl_MFS") = lContract.MFS
    dblSpread = lContract.MFS
    End If
    
'Calculate Credit_Runoff with old Cash_Flow to eliminate wrong runoffs which are only needed for MFS calculation
Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, _
strMM, strSW, intCompounding_Frequency, intAnnualized)
    
End If

arrCalculation = fctCalculation_Generation(datePayout_Date, intPayment_Frequency, _
                dateFirst_Instalment_Date_Input, _
                dblInititial_Direct_Cost, _
                dblSubsidies, intCredit_Term, _
                lContract, _
                dblMSRP, dblAdd_Coll, _
                strUS_OL, dblNOM_CR, lContract.IRR, arrDCF_Spread_Range_1(), _
                intRepricing_Term, intInterest_Type_num, arrPD_Matrix(), dblFinal_PD, dblFinal_PD2, strBasel_Type, _
                dblManual_LGD, strRV_Balloon, dblcontracted_RV, dblEC_RVR, dblNAF, dblEC_HC, dblEC_OPR, _
                dblScaling_Factor, dblHurdle_Rate, dblFundingR, dblSpread, dblNIBL, dblRV_Enhancements, _
                dblDeal_Rate, dblManual_MFR_Interest, strInterest_Type, dblManual_MFR_Spread, dbl_CTYRisk, strRORACTargetCase, _
                dblAdd_Coll2, strAdd_Coll_Type, arrResults())
                
If strRORACTargetCase = "Yes" Then
    intCounterRORAC = intCounterRORAC + 1
    GoTo RORACTargetCase
End If

'new fixed cash flow for fixed rate IDC/Subsidy contracts in Turkey
'In Turkey they need the split of the IRR effect for IDC and subsidies separately. Hence, an additional IRR only including subsidies
'is calculated. The difference between the new IRR ([mdl_IRR_excl_IDC]) and the customer rate yields the effect from subsidies and the difference between
'the new IRR and the old IRR ([Mdl_IRR]) yields the effect from IDCs. For more details of the calculation please refer to the Local Sheet in the Turkish version of the DFS RORAC Tool.

If ([IDC] <> 0 Or dblSubsidies <> 0) And [Interest_Type] = "Fix" And [Country_Short] = "TUR" Then
arrCF(0, 2) = arrCF(0, 2) + [IDC]
    
    Call sub_CalcCF(lContract, arrCF, laCurve, laSpreads, dblCalculation_Date, _
    strMM, strSW, intCompounding_Frequency, intAnnualized)

[mdl_IRR_excl_IDC] = lContract.IRR

End If

'calc write PV lost interest for MEX if interest and instalment free periods
'That is an older solution for principal only periods in Mexico. On the Mexican Local Sheet there is the opportunity to input so called
'grace periods. These are periods in the beginning of a contract where the customer does not have to pay interest. In this case the lost interest
'is approximated on the Index sheet (see calculation in range [PV_Lost_Interest]) and then transfered as a negative initial fee in to the Input Mask
'and the contract is calculated again. The effect will then been shown in the IDC line.
'On the cash flow analysis sheet there is a Zero cash flow payment shown for all grace periods, because this is how the Mexicans want it to be shown to
'the customer. However in the background amortization of the contract is based on the regular amortization schedule.
'This functionality could probably been taken out and the grace periods in Mexico been handled like principal only periods on the manual cash flow
'sheet. However, this will probably only been accepted by the entity if the user of the tool has not to change his input routine (ie just selecting grace
'periods on the Local Sheet).

Dim temp_counter As Integer
If [Country_Short] = "MEX" And [Grace_Period] <> 0 And temp_counter <> 1 Then
Application.Calculate
Worksheets("New Input Mask").Range("H39").value = Worksheets("New Input Mask").Range("H39").value - [PV_Lost_Interest]
Application.Calculate
temp_counter = 1
GoTo start
End If

'calculate customer IRR based on fixed margin for MBCW France
'Charterway contracts in France are priced/bought by MBFS France at a fixed Gross Interest Margin. The margin is either a standard which is reviewed
'on a irregular basis or set manually. The option to choose between the two alternatives was implemented on the French Local Sheet. The final margin value
'is stored in [Margin_CW_France]. Based on this value and the CoD a customer rate is calculated and written into the Input Mask.
If [Company] = "(FRA) Mercedes-Benz Charterway" And temp_counter <> 1 Then
dblNOM_CR = lContract.MFR + lContract.MFS + [Margin_CW_France]
Worksheets("New Input Mask").Range("H54").value = dblNOM_CR * 100
Application.Calculate
temp_counter = 1
GoTo start
End If

End Sub








--- Macro File: Frm_Customer.frm ---
Attribute VB_Name = "Frm_Customer"
Attribute VB_Base = "0{9F1C98CD-BE31-401F-BDEE-94C0522A8187}{88535173-3DFD-40F0-A1FC-E14AB5964196}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Public intEntity_Row As Integer

Private Sub CommandButton1_Click()

Dim i As Integer
Dim intRow_Save As Integer
 
'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

intRow_Save = 1

'Validate entered values
For i = 1 To 10
    If Frm_Customer("textbox" & Trim(Str(i))).value <> "" And Frm_Customer("ComboBox" & Trim(Str(i))).value = "" Then
            Frm_Customer("ComboBox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a Borrower Type"
            Frm_Customer("ComboBox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
    End If
Next

'Delete old Customers, if all entered values aare correct
For i = 1 To 10
   Worksheets("Data_Entities").Cells(intEntity_Row + i, posCustBorType_DE).value = ""
   Worksheets("Data_Entities").Cells(intEntity_Row + i, posCustBorType_DE + 1).value = ""
Next

'Save new Customers and Borrower Types
For i = 1 To 10
    If Frm_Customer("textbox" & Trim(Str(i))).value <> "" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posCustBorType_DE).value = Frm_Customer("textbox" & Trim(Str(i))).value
        Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posCustBorType_DE + 1).value = Frm_Customer("ComboBox" & Trim(Str(i))).value
        intRow_Save = intRow_Save + 1
    End If
Next

'MsgBox "Customer Update Succesful"
Unload Me

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

Private Sub UserForm_activate()

Dim i As Integer

'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

For i = 1 To 10
    Frm_Customer("textbox" & Trim(Str(i))).value = Worksheets("Data_Entities").Cells(intEntity_Row + i, posCustBorType_DE).value
    Frm_Customer("ComboBox" & Trim(Str(i))).value = Worksheets("Data_Entities").Cells(intEntity_Row + i, posCustBorType_DE + 1).value
Next

End Sub


--- Macro File: Frm_Opex.frm ---
Attribute VB_Name = "Frm_Opex"
Attribute VB_Base = "0{65C01EE6-33D9-4C1A-A17A-C5D2A845C0CE}{EF445C17-CC18-4C3F-BB9F-8F49B05DACBC}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Public intEntity_Row As Integer
Private Sub CommandButton1_Click()

Dim intRow_Save As Integer
Dim strOS_Dec_Separator As String
Dim strApp_Dec_Separator As String

 
'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

intRow_Save = 1
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'Validate entered OPEX Values
For i = 1 To 25
    If Application.UseSystemSeparators = True Then
        varTB_Value = Frm_Opex("textbox" & Trim(Str(i)) & "b").value
    Else
        varTB_Value = Replace(Frm_Opex("textbox" & Trim(Str(i)) & "b").value, strApp_Dec_Separator, strOS_Dec_Separator)
    End If
    If Frm_Opex("textbox" & Trim(Str(i))).value <> "" And (varTB_Value = "" Or varTB_Value < 0 Or Not IsNumeric(varTB_Value) Or CDbl(varTB_Value > 100)) Then
        Frm_Opex("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 0, 0)
        MsgBox "Please enter a correct OPEX Ratio"
        Frm_Opex("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
Next

'Delete Old OPEX Segements
For i = 1 To 25
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posOPEX_DE).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posOPEX_DE + 1).value = ""
Next

'Save new Opex Segements
For i = 1 To 25
    If Frm_Opex("textbox" & Trim(Str(i))).value <> "" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posOPEX_DE).value = Frm_Opex("textbox" & Trim(Str(i))).value
        If Application.UseSystemSeparators = True Then
            Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posOPEX_DE + 1).value = CDbl(Frm_Opex("textbox" & Trim(Str(i)) & "b").value / 100)
        Else
            Worksheets("Data_Entities").Cells(intEntity_Row + intRow_Save, posOPEX_DE + 1).value = CDbl(Replace(Frm_Opex("textbox" & Trim(Str(i)) & "b").value, strApp_Dec_Separator, strOS_Dec_Separator)) / 100
        End If
    intRow_Save = intRow_Save + 1
    End If
Next

'MsgBox "OPEX Update Succesful"

Unload Me

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

Private Sub UserForm_activate()
Dim i As Integer
Dim strOS_Dec_Separator As String
Dim strApp_Dec_Separator As String

intEntity_Row = fct_entity_data_position()
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

For i = 1 To 25
    Frm_Opex("textbox" & Trim(Str(i))).value = Worksheets("Data_Entities").Cells(intEntity_Row + i, posOPEX_DE).value
    If Worksheets("Data_Entities").Cells(intEntity_Row + i, posOPEX_DE + 1).value = "" Then
        Frm_Opex("textbox" & Trim(Str(i)) & "b").value = Worksheets("Data_Entities").Cells(intEntity_Row + i, posOPEX_DE + 1).value
    Else
        If Application.UseSystemSeparators = True Then
            Frm_Opex("textbox" & Trim(Str(i)) & "b").text = Worksheets("Data_Entities").Cells(intEntity_Row + i, posOPEX_DE + 1).value * 100
        Else
            Frm_Opex("textbox" & Trim(Str(i)) & "b").text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + i, posOPEX_DE + 1).value * 100, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    End If
Next
End Sub



--- Macro File: Frm_Int_Pd.frm ---
Attribute VB_Name = "Frm_Int_Pd"
Attribute VB_Base = "0{F8B100C7-63D2-4B81-B748-273E0F5D0AB3}{838C72AE-6DC5-450D-9EC4-49EB5FC7BAD0}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Public intEntity_Row As Integer
Private strOS_Dec_Separator As String
Private strApp_Dec_Separator As String
Private Sub CommandButton1_Click()

Dim i As Integer
Dim intColumn_Save As Integer

intColumn_Save = 0

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

'Validate First PD Curve entries
If ComboBox1.value <> "" Then
    For i = 2 To 11
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If Not IsNumeric(Frm_Int_Pd("textbox" & Trim(Str(i))).value) Or varTB < 0 Or varTB > 100 Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct PD Value"
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
End If

'Validate Second PD Curve entries
If ComboBox2.value <> "" Then
    For i = 13 To 22
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If Not IsNumeric(Frm_Int_Pd("textbox" & Trim(Str(i))).value) Or varTB < 0 Or varTB > 100 Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct PD Value"
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
End If

'Validate Third PD Curve entries
If ComboBox3.value <> "" Then
   For i = 24 To 33
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If Not IsNumeric(Frm_Int_Pd("textbox" & Trim(Str(i))).value) Or varTB < 0 Or varTB > 100 Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct PD Value"
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
End If

'Validate Fourth PD Curve entries
If ComboBox4.value <> "" Then
   For i = 35 To 44
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If Not IsNumeric(Frm_Int_Pd("textbox" & Trim(Str(i))).value) Or varTB < 0 Or varTB > 100 Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct PD Value"
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
End If

'Validate Fifth PD Curve entries
If ComboBox5.value <> "" Then
   For i = 46 To 55
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        If Not IsNumeric(Frm_Int_Pd("textbox" & Trim(Str(i))).value) Or varTB < 0 Or varTB > 100 Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a correct PD Value"
            Frm_Int_Pd("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
End If

'Delete old PD-Curves
For i = 0 To 10
        Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntPD_DE).value = ""
        Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntPD_DE + 1).value = ""
        Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntPD_DE + 2).value = ""
        Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntPD_DE + 3).value = ""
        Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntPD_DE + 4).value = ""
Next

'Save First PD Curve
If ComboBox1.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + intColumn_Save) = ComboBox1.value
    For i = 2 To 11
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        Worksheets("Data_Entities").Cells(intEntity_Row + i - 1, posIntPD_DE + intColumn_Save).value = CDbl(varTB)
    Next
        intColumn_Save = intColumn_Save + 1
End If

'Save Second PD Curve
If ComboBox2.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + intColumn_Save) = ComboBox2.value
    For i = 13 To 22
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        Worksheets("Data_Entities").Cells(intEntity_Row + i - 12, posIntPD_DE + intColumn_Save).value = CDbl(varTB)
    Next
    intColumn_Save = intColumn_Save + 1
End If


'Save Third PD Curve
If ComboBox3.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + intColumn_Save) = ComboBox3.value
    For i = 24 To 33
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        Worksheets("Data_Entities").Cells(intEntity_Row + i - 23, posIntPD_DE + intColumn_Save).value = CDbl(varTB)
    Next
    intColumn_Save = intColumn_Save + 1
End If

'Save Fourth PD Curve
If ComboBox4.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + intColumn_Save) = ComboBox4.value
    For i = 35 To 44
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        Worksheets("Data_Entities").Cells(intEntity_Row + i - 34, posIntPD_DE + intColumn_Save).value = CDbl(varTB)
    Next
    intColumn_Save = intColumn_Save + 1
End If

'Save Fifth PD Curve
If ComboBox5.value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + intColumn_Save) = ComboBox5.value
    For i = 46 To 55
        If Application.UseSystemSeparators = True Then
            varTB = Frm_Int_Pd("textbox" & Trim(Str(i))).value
        Else
            varTB = Replace(Frm_Int_Pd("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        Worksheets("Data_Entities").Cells(intEntity_Row + i - 45, posIntPD_DE + intColumn_Save).value = CDbl(varTB)
    Next
    intColumn_Save = intColumn_Save + 1
End If
'MsgBox "Internal PD Update Succesful"
Unload Me
End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

Private Sub UserForm_activate()

Dim i As Integer


strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

intEntity_Row = fct_entity_data_position()

'Load First Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE).value <> "" Then
    ComboBox1.value = Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE).value
    For i = 2 To 11
        If Application.UseSystemSeparators = True Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row + i - 1, posIntPD_DE).value
        Else
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + i - 1, posIntPD_DE).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    Next
End If

'Load Second Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 1).value <> "" Then
    ComboBox2.value = Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 1).value
    For i = 13 To 22
        If Application.UseSystemSeparators = True Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row + i - 12, posIntPD_DE + 1).value
        Else
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + i - 12, posIntPD_DE + 1).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    
    Next
End If

'Load Third Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 2).value <> "" Then
    ComboBox3.value = Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 2).value
    For i = 24 To 33
        If Application.UseSystemSeparators = True Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row + i - 23, posIntPD_DE + 2).value
        Else
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + i - 23, posIntPD_DE + 2).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    Next
End If

'Load Fourth Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 3).value <> "" Then
    ComboBox4.value = Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 3).value
    For i = 35 To 44
        If Application.UseSystemSeparators = True Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row + i - 34, posIntPD_DE + 3).value
        Else
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + i - 34, posIntPD_DE + 3).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    Next
End If

'Load Fifth Interests and Spreads Curve
If Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 4).value <> "" Then
    ComboBox5.value = Worksheets("Data_Entities").Cells(intEntity_Row, posIntPD_DE + 4).value
    For i = 46 To 55
        If Application.UseSystemSeparators = True Then
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row + i - 45, posIntPD_DE + 4).value
        Else
            Frm_Int_Pd("textbox" & Trim(Str(i))).text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + i - 45, posIntPD_DE + 4).value, strOS_Dec_Separator, strApp_Dec_Separator)
        End If
    Next
End If
End Sub


--- Macro File: Frm_Dep_Curve.frm ---
Attribute VB_Name = "Frm_Dep_Curve"
Attribute VB_Base = "0{7D928A2B-52C6-43B1-A76E-F2C3BFE3112A}{E57BF278-19A3-45B4-B4BB-726360215ABA}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Public intEntity_Row As Integer
Private strOS_Dec_Separator As String
Private strApp_Dec_Separator As String
Private Sub CommandButton1_Click()

Dim i As Integer
Dim intColumn_Save As Integer

intColumn_Save = 1

'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'Validate entered form values
For i = 1 To 15
    If Frm_Dep_Curve("textbox" & Trim(Str(i))).value <> "" Then
        If Frm_Dep_Curve("ComboBox" & Trim(Str(i))).value = "" Then
            Frm_Dep_Curve("ComboBox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
            MsgBox "Please add a Depreciation Curve Type"
            Frm_Dep_Curve("ComboBox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
            Exit Sub
        ElseIf Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").value = "" Or Not IsNumeric(Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").value) Then
            Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 0, 0)
            MsgBox "Please add a A-Parameter"
            Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 255, 255)
            Exit Sub
        ElseIf Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").value = "" Or Not IsNumeric(Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").value) Then
            Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").BackColor = RGB(255, 0, 0)
            MsgBox "Please add a B-Parameter"
            Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
  End If
Next

'Delete old Depreciation curves
For i = 0 To 14
    Worksheets("Data_Entities").Cells(intEntity_Row, posDepCurve_DE + i).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + 1, posDepCurve_DE + i).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + 2, posDepCurve_DE + i).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + 3, posDepCurve_DE + i).value = ""
Next

'Save New Depreciation Curves
For i = 1 To 15
    If Frm_Dep_Curve("textbox" & Trim(Str(i))).value <> "" Then
        Worksheets("Data_Entities").Cells(intEntity_Row, posDepCurve_DE - 1 + intColumn_Save).value = Frm_Dep_Curve("textbox" & Trim(Str(i))).value
        Worksheets("Data_Entities").Cells(intEntity_Row + 1, posDepCurve_DE - 1 + intColumn_Save).value = Frm_Dep_Curve("ComboBox" & Trim(Str(i))).value
        If Application.UseSystemSeparators = True Then
            Worksheets("Data_Entities").Cells(intEntity_Row + 2, posDepCurve_DE - 1 + intColumn_Save).value = CDbl(Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").value)
            Worksheets("Data_Entities").Cells(intEntity_Row + 3, posDepCurve_DE - 1 + intColumn_Save).value = CDbl(Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").value)
        Else
            Worksheets("Data_Entities").Cells(intEntity_Row + 2, posDepCurve_DE - 1 + intColumn_Save).value = CDbl(Replace(Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").value, strApp_Dec_Separator, strOS_Dec_Separator))
            Worksheets("Data_Entities").Cells(intEntity_Row + 3, posDepCurve_DE - 1 + intColumn_Save).value = CDbl(Replace(Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").value, strApp_Dec_Separator, strOS_Dec_Separator))
        End If
    intColumn_Save = intColumn_Save + 1
    End If
Next

'MsgBox "Depreciation Curve Update Succesful"
Unload Me

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

'Sub to prefill textboxes with current curve data
Private Sub UserForm_activate()

Dim i As Integer

intEntity_Row = fct_entity_data_position()
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

For i = 1 To 15
    Frm_Dep_Curve("textbox" & Trim(Str(i))).text = Worksheets("Data_Entities").Cells(intEntity_Row, posDepCurve_DE - 1 + i).value
    Frm_Dep_Curve("ComboBox" & Trim(Str(i))).value = Worksheets("Data_Entities").Cells(intEntity_Row + 1, posDepCurve_DE - 1 + i).value
    If Application.UseSystemSeparators = True Then
        Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").text = Worksheets("Data_Entities").Cells(intEntity_Row + 2, posDepCurve_DE - 1 + i).value
        Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").text = Worksheets("Data_Entities").Cells(intEntity_Row + 3, posDepCurve_DE - 1 + i).value
    Else
        Frm_Dep_Curve("textbox" & Trim(Str(i)) & "b").text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + 2, posDepCurve_DE - 1 + i).value, strOS_Dec_Separator, strApp_Dec_Separator)
        Frm_Dep_Curve("textbox" & Trim(Str(i)) & "c").text = Replace(Worksheets("Data_Entities").Cells(intEntity_Row + 3, posDepCurve_DE - 1 + i).value, strOS_Dec_Separator, strApp_Dec_Separator)
    End If
Next
End Sub


--- Macro File: mdl_global.bas ---
Attribute VB_Name = "mdl_global"
'global constants that say at which columns the respective parameter information can be found on the data entities sheet
Global Const posCustBorType_DE As Integer = 2
Global Const posOPEX_DE As Integer = 4
Global Const posProduct_DE As Integer = 6
Global Const posIntPD_DE As Integer = 10
Global Const posIntSpr_DE As Integer = 16
Global Const posDepCurve_DE As Integer = 20
Global Const posAddPara_DE As Integer = 35
Global Const posPLSPara_DE As Integer = 39
Global Const posListPrice_DE As Integer = 60
Global Const posStartDPCur_DE As Integer = 61
Global Const posOPEXField = 63
Global Const posOPEXFormula = 64
Global Const posPDDigitAllowed = 65
Global Const posRecoveryUnsecured = 66
Global Const posCoR = 67
Global Const posEC_CTYR = 68
Global Const posPDAddon1_CoR = 69
Global Const posPDAddon2_EC = 70
Global Const posEffMatBlended = 71
Global Const posOneEC = 78
Private Const KEYEVENTF_KEYUP = &H2
Private Const VK_MENU = &H12
Private Const lngMargin = 1 'Breite der Seitenränder in cm
Global int_Anzstreams As Integer
Global bol_ChangeEvent As Boolean
Global Const pw_DB = "teamwork"
Public unhideFlag As String

'Bitte Hauptversion hier ändern
Global Const APPVERSION = "7.17"

#If VBA7 Then    ' VBA7
Private Declare PtrSafe Function MapVirtualKey Lib "user32.dll" Alias "MapVirtualKeyA" ( _
     ByVal wCode As Long, _
     ByVal wMapType As Long) As Long
Private Declare PtrSafe Sub keybd_event Lib "user32.dll" ( _
     ByVal bVk As Byte, _
     ByVal bScan As Byte, _
     ByVal dwFlags As Long, _
     ByVal dwExtraInfo As Long)
     
'Declare Function to read operating system decimal separator...
Declare PtrSafe Function GetProfileString Lib "kernel32" _
                        Alias "GetProfileStringA" (ByVal lpAppName As String, _
                                                   ByVal lpKeyName As String, _
                                                   ByVal lpDefault As String, _
                                                   ByVal lpReturnedString As String, _
                                                   ByVal nSize As Long) As Long
                                                   
#Else    'Downlevel when using previous version of VBA7

Private Declare Function MapVirtualKey Lib "user32.dll" Alias "MapVirtualKeyA" ( _
     ByVal wCode As Long, _
     ByVal wMapType As Long) As Long
Private Declare Sub keybd_event Lib "user32.dll" ( _
     ByVal bVk As Byte, _
     ByVal bScan As Byte, _
     ByVal dwFlags As Long, _
     ByVal dwExtraInfo As Long)
     
'Declare Function to read operating system decimal separator...
Declare Function GetProfileString Lib "kernel32" _
                        Alias "GetProfileStringA" (ByVal lpAppName As String, _
                                                   ByVal lpKeyName As String, _
                                                   ByVal lpDefault As String, _
                                                   ByVal lpReturnedString As String, _
                                                   ByVal nSize As Long) As Long
#End If

'sub to print LGD chart without user dialog on default printer with page maximizing
Public Sub prcPrintForm(objForm As Object)
    Dim intAltScan As Integer, intIndex As Integer
    Application.ScreenUpdating = False
    intAltScan = MapVirtualKey(VK_MENU, 0&)
    keybd_event VK_MENU, intAltScan, 0&, 0&
    keybd_event vbKeySnapshot, 0&, 0&, 0&
    DoEvents
    keybd_event VK_MENU, intAltScan, KEYEVENTF_KEYUP, 0&
    ThisWorkbook.Worksheets.Add
    Rows.RowHeight = 3
    Columns.ColumnWidth = 0.83
    With ActiveSheet
        .Paste
        With .PageSetup
            .Orientation = IIf(objForm.Width > objForm.Height, xlLandscape, xlPortrait)
            .LeftMargin = Application.CentimetersToPoints(lngMargin)
            .RightMargin = Application.CentimetersToPoints(lngMargin)
            .TopMargin = Application.CentimetersToPoints(lngMargin)
            .BottomMargin = Application.CentimetersToPoints(lngMargin)
            .HeaderMargin = Application.CentimetersToPoints(0)
            .FooterMargin = Application.CentimetersToPoints(0)
            .CenterVertically = True
            .CenterHorizontally = True
            .Zoom = 10
            For intIndex = 1 To 3
                Do Until ExecuteExcel4Macro("Get.Document(50)") > 1
                    .Zoom = .Zoom + Choose(intIndex, 50, 10, 1)
                Loop
                .Zoom = .Zoom - Choose(intIndex, 50, 10, 1)
            Next
        End With
        .PrintOut
        Application.DisplayAlerts = False
        .Delete
        Application.DisplayAlerts = True
    End With
    Application.ScreenUpdating = True
End Sub

'generic sub to protect Input mask, set password and standard application parameter to prevent for user changes
Public Sub protectInput()

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("New Input Mask").Protect Password:="Blattschutz"

End Sub

'generic sub to unprotect Input mask and standard application parameter to change work with sheet
Public Sub unprotectInput()

Worksheets("New Input Mask").Unprotect Password:="Blattschutz"
Application.Calculate
With Application
        .Calculate
        .Calculation = xlManual
        .MaxChange = 0.001
        .ScreenUpdating = False
End With
ActiveWorkbook.PrecisionAsDisplayed = False

End Sub

'Determines the position of the selected Entity within the Data Entities Sheet
Public Function fct_entity_data_position() As Integer
Dim intPosition As Integer

intPosition = 0

For i = 2 To 58
    If Worksheets("Index").Range("Company") = Worksheets("Data_Entities").Cells(i, 3) Then
        intPosition = Worksheets("Data_Entities").Cells(i, 4).value
        Exit For
    End If
Next

fct_entity_data_position = intPosition
End Function


'Function to read operating system decimal separator...
Public Function fct_SystemSetting(pstrType As String) As String

'// Description    :   get setting of system
'//                    (sShortDate for date, sDecimal for decimal devider, ...)

    Dim lstrHelp As String
    
    lstrHelp = String$(255, 0)
    If GetProfileString("Intl", pstrType, "", lstrHelp, Len(lstrHelp)) Then
        lstrHelp = Left(lstrHelp, InStr(lstrHelp, Chr(0)) - 1)
    Else
        lstrHelp = ""
    End If

    fct_SystemSetting = lstrHelp

End Function

'sub to check if all inputs are valid
Public Function fct_checkInput() As Boolean
Dim intLastInst As Integer

'Stops Calculation if no Entity is selected
If Worksheets("New Input Mask").Range("L5").value = "Please choose an Entity" Then
    MsgBox "Please choose an Entity"
    fct_checkInput = False
    Exit Function
End If

'Determine position of current entity in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

'Stops Calculation if no Data for selected Entity are available
If intEntity_Row = 0 Then
    MsgBox "No Parameters for this Entity available"
    Call protectInput
    Exit Function
End If

'Stops Calculation if no Customer Type is selected
If Worksheets("New Input Mask").Range("E8").value = "" Then
    MsgBox "Please Select a Customer Type"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Internal Rating curve is available
If Worksheets("New Input Mask").Range("E10").value <> "" And Worksheets("New Input Mask").Range("E10").value <> "Not Rated" And [Internal_PD_Curve_Available] = "No" And [Is_Bank_Branch_deal] = "No" Then
    MsgBox "No Internal Rating Curve for Customer Type available"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if No PD can be determined
If Worksheets("New Input Mask").Range("E10").value = "" And Worksheets("New Input Mask").Range("E12").value = "" Then
    MsgBox "Please Enter a Internal Rating or a Manual PD for the Customer"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Vehicle Number is added
If Worksheets("New Input Mask").Range("L17").value = "" Then
    MsgBox "Please enter the number of Vehicles you want to finance"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if Add. Coll. Type is selected
If Worksheets("New Input Mask").Range("L19").value = "" And Worksheets("New Input Mask").Range("Q19").value = "" Then
    Worksheets("New Input Mask").Range("Q19").value = Worksheets("Index").Range("AA2").value
ElseIf Worksheets("New Input Mask").Range("L19").value <> "" And Worksheets("New Input Mask").Range("Q19").value = "" Then
    MsgBox "Please select an Add. Coll. Type"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Asset Depreciation Curve is selected
If Worksheets("New Input Mask").Range("E19").value = "" Then
    MsgBox "Please select an Asset Valuation Curve"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Financial Product Type is selected
If Worksheets("New Input Mask").Range("E25").value = "" Then
    MsgBox "Please select a Financial Product"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no list price or manual LGD is added
If [manual_LGD] = -1 And [List_Price_Parameter] = "yes" And [List_Price] = "" Then
    MsgBox "List Price Field is empty. Average Discount Assumptions and actual Sales Price will be used."
End If

'Stops Calculation if no Deal Currency is selected
If Worksheets("New Input Mask").Range("D27").value = "" Then
    MsgBox "Please select a Deal Currency"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no OPEX Segment is selected
If Worksheets("New Input Mask").Range("G27").value = "" And (Worksheets("Index").Range("Opex_Parameter").value = "Yes" Or Worksheets("Index").Range("Opex_Parameter").value = "yes") Then
    MsgBox "Please select an OPEX Segment"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Sales Price is selected
If Worksheets("New Input Mask").Range("h29").value = "" Then
    MsgBox "Please enter a Sales Price"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Credit Term is selected
If Worksheets("New Input Mask").Range("H47").value = "" Then
    MsgBox "Please add a Credit Term"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Interest Rate Type is added
If Worksheets("New Input Mask").Range("E49").value = "" Then
    MsgBox "Please select an Interest Rate Type"
    fct_checkInput = False
    Exit Function
End If

'Set whether Balloon/RV comprises last installment
'If comprises, then cell is set to "no", if does not comprise, then set to "yes"
If Worksheets("Index").Range("Last_Instalment").value = "no" Then
    intLastInst = 2
Else
    intLastInst = 1
End If

'Stops Calculation if interest only period is higher then credit term
If CInt(Worksheets("Index").Range("Interest_Only_Period").value) > (CInt(Worksheets("Index").Range("Maturity").value) - intLastInst) Then
    MsgBox "Please reduce Interest Only Period."
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Repricing Term is added
If Worksheets("New Input Mask").Range("H49").value = "" And Not Worksheets("New Input Mask").Range("e45").value = "Fix" Then
    MsgBox "Please select a Repricing Term"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Payment Frequency is added
If Worksheets("New Input Mask").Range("E52").value = "" Then
    MsgBox "Please select a Payment Frequency"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Payment Mode set
If Worksheets("New Input Mask").Range("H52").value = "" Then
    MsgBox "Please add a Payment Mode"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if Payment Frequency > Credit Term
If (CInt(Worksheets("Index").Range("Maturity").value) - CInt(Worksheets("Index").Range("Payment_Frequency").value) < 0) Then
    MsgBox "No cashflows. Please change either Credit Term or Payment Frequency"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if Payment Mode is "In Advance" and Payment Frequency > Credit Term
If (CInt(Worksheets("Index").Range("Maturity").value) - CInt(Worksheets("Index").Range("Payment_Frequency").value)) = 0 Then
    If Worksheets("Index").Range("Payment_Mode").value = "In Advance" And Worksheets("Index").Range("Cont_RV").value = 0 Then
        MsgBox "No cashflows. Please change either Credit Term, Payment Frequency or Payment Mode"
        fct_checkInput = False
        Exit Function
    ElseIf Worksheets("Index").Range("Payment_Mode").value = "In Arrears" And Worksheets("Index").Range("Last_Instalment").value = "no" Then
        MsgBox "No cashflows. Please change either Credit Term, Payment Frequency or Payment Mode"
        fct_checkInput = False
        Exit Function
    End If
End If

'Stops Calculation if no Payout Date is added
If Worksheets("New Input Mask").Range("E54").value = "" Then
    MsgBox "Please add a Payout Date"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if no Customer or Buy Rate is added
If Worksheets("New Input Mask").Range("H54").value = "" Then
    MsgBox "Please add a Customer or DFS Buy Rate"
    fct_checkInput = False
    Exit Function
End If

'Stops Calculation if fixing period for variable contracts is higher than credit term
If Worksheets("Index").Range("Maturity").value <= [Interest_Type_num] Then
    MsgBox "Credit Term cannot be shorter than fixing period for variable Interest Rate." & vbCrLf & _
    "If the Fixing Period equals the Credit Term, please change " & Chr(34) & "Interest Rate Type" & Chr(34) & " to " & Chr(34) & "Fix" & Chr(34) & "."
    fct_checkInput = False
    Exit Function
End If

'Stops calculation if Accelerated Payment is active and Target is lower than Balloon or bigger than NAF
If (Worksheets("Index").Range("End_Value_Acc_Payment").value < [Cont_RV] Or Worksheets("Index").Range("End_Value_Acc_Payment").value > [NAF]) And [Accelerated_Payment_Flag] = 1 Then
    MsgBox "Target Amount of Accelerated Payment is smaller than Balloon or greater than Finance Amount. Please change."
    fct_checkInput = False
    Exit Function
End If

'Stops calculation if amount for Add Colleteral is added but no type
If Worksheets("New Input Mask").Range("L19").value <> 0 And Worksheets("New Input Mask").Range("U19").value = "" Then
    MsgBox "Please enter Add. Coll. Type or delete the Add. Coll. Amount."
    fct_checkInput = False
    Exit Function
End If


Application.Calculate
fct_checkInput = True
End Function

'function for us implemented by Carsten Sturmann
Public Function GetUserName()
    GetUserName = Application.UserName
End Function

'sub to label and unhide LGD Button
'exception for US: Button will not be shown at all
Public Sub LGD_button_CalcDate()

If [Country_Short] <> "USA" Then
    Worksheets("New Input Mask").CommandButton11.Visible = True
    If [final_PD] = 0 Then
        Worksheets("New Input Mask").CommandButton11.Caption = "PLS: " & Application.Round([Mdl_Average_LGD], 4) * 100 & "%"
    Else
        Worksheets("New Input Mask").CommandButton11.Caption = "PLS: " & Application.Round(([Mdl_CoCR]) / [final_PD], 6) * 10000 & "%"
    End If
End If

If Worksheets("INDEX").Range("Country_short") = "GBR" And Worksheets("INDEX").Range("Is_Bank_Branch_deal") Then
       Worksheets("New Input Mask").CommandButton11.Caption = "PLS: " & Application.Round([Mdl_Average_LGD], 4) * 100 & "%"
End If

Worksheets("Index").Range("M148") = Str(Date)
Worksheets("Index").Range("M155") = Str((Worksheets("Index").Range("e200").value))
End Sub


'special function for france to calculate the interest rate effect of a delayed payment of the sales price by MBFS France to the MPC
Public Sub France()
Application.Calculate
Worksheets("New Input Mask").Range("H39").value = 0
If Worksheets("Local_Sheet").Range("B2") = "Qui" Then
    Worksheets("New Input Mask").Range("H39").value = [NAF] * ([mdl_MFR] + [mdl_MFS]) / 360 * Worksheets("Local_Sheet").Range("B3").value 'for the delayed period the MBFS France saves the funding cost (MFR + MFS) on the Finance Amount;
    'the absolut amount from this calculation is written into the upfront payment section (here positive effect like subsidy) in the New Input Mask;
    'for reasons of simplicity the amount is not discounted
End If

Worksheets("New Input Mask").ComboBox5.value = Worksheets("Index").Range("AY2").value
Worksheets("New Input Mask").Range("H41").value = 0
If Worksheets("Local_Sheet").Range("B9").value = "Qui" Then
Worksheets("New Input Mask").Range("H41").value = Worksheets("Local_Sheet").Range("B26") * 100
End If
Worksheets("New Input Mask").Range("H41").value = Worksheets("New Input Mask").Range("H41").value + (Worksheets("Local_Sheet").Range("B34").value * 100) - ([Commission_France] * 100)
Worksheets("New Input Mask").Range("H39").value = Worksheets("New Input Mask").Range("H39").value + Worksheets("Local_Sheet").Range("B35").value
Call unprotectInput

Dim zelle As Range
Dim lItem As Integer
Dim AnzMonths As Integer
Dim intLastInst
Dim bolInp_OK As Boolean

lItem = 0


bolInp_OK = fct_checkInput()

If bolInp_OK = False Then
    Call protectInput
    Exit Sub
End If

'MessageBox If stored Interest and Spreads older than one week or one month (Exception for Mexico, Spain and Thailand)
If Worksheets("Index").Range("Expiry_Date_IS") < Date And [Country_Short] <> "MEX" Then
    If [Country_Short] = "RUS" Then
        MsgBox ("Stored Interests and Spreads are older than one week (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    ElseIf [Country_Short] = "THA" Then
        MsgBox ("Stored Interests and Spreads are older than two weeks (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    Else
        MsgBox ("Stored Interests and Spreads are older than one month (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    End If
End If

'MessageBox If stored Manual Cash Flow is used
If Worksheets("Index").Range("Manual_CF_Flag") = 1 Then
    MsgBox ("Manual Cash Flow is active and will be considered for calculation!")
End If

'MessageBox If more than one collateral is added
If Worksheets("Index").Range("C323").value <> "" Or Worksheets("Index").Range("C324").value <> "" Then
    MsgBox ("More than one additional collateral was added and will be considered for calculation!")
End If

'MessageBox If stored Acceleraded Payment is used
If Worksheets("Index").Range("Accelerated_Payment_Flag") = 1 Then
    MsgBox ("Accelerated Payment is active and will be considered for calculation!")
    Worksheets("Index").Range("write_mcf").value = "yes"
    Worksheets("Index").Range("Accelerated_Payment_Flag") = 0
    Call prcStartCalculation
    Worksheets("Index").Range("write_mcf").value = "no"
    Worksheets("Index").Range("Accelerated_Payment_Flag") = 1
    Application.Calculate
End If


'Start RORAC-Calculation
Call prcStartCalculation

Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "RORAC was successfully calculated"

End Sub


Public Sub France_Dealer()
Dim entryvalue As Byte
'depending on the finance product type different range values from the french Local Sheet are copied into the Input Mask; usually its the interest rate and the flat fees
'which depend on the finance product type; they get calculated on the Local Sheet based on the same formulas that were used in the past in the french
'Dealer pricing tool
Select Case [Financial_Product_Type]
                    
            'Bank Branch Products:
            
            Case "WFS New Vehicle - Bank Branch"
                entryvalue = MsgBox(prompt:="Do you want to take over effective customer lending rate and flat fees from Local Sheet?", Buttons:=vbYesNo)
                If entryvalue = vbNo Then
                Exit Sub
                End If
            Worksheets("New Input Mask").Range("H54").value = Worksheets("Local_Sheet").Range("C212").value * 100
            Worksheets("New Input Mask").ComboBox5.value = Worksheets("Index").Range("AY2").value
            Worksheets("New Input Mask").Range("H41").value = Worksheets("Local_Sheet").Range("C213").value * 100
        Case "Demo vehicles - Bank Branch"
                entryvalue = MsgBox(prompt:="Do you want to take over effective customer lending rate and flat fees from Local Sheet?", Buttons:=vbYesNo)
                If entryvalue = vbNo Then
                Exit Sub
                End If
            Worksheets("New Input Mask").Range("H54").value = Worksheets("Local_Sheet").Range("C214").value * 100
            Worksheets("New Input Mask").ComboBox5.value = Worksheets("Index").Range("AY2").value
            Worksheets("New Input Mask").Range("H41").value = Worksheets("Local_Sheet").Range("C215").value * 100
        Case "Spare Parts - Bank Branch"
                entryvalue = MsgBox(prompt:="Do you want to take over effective customer lending rate and flat fees from Local Sheet?", Buttons:=vbYesNo)
                If entryvalue = vbNo Then
                Exit Sub
                End If
            Worksheets("New Input Mask").Range("H54").value = Worksheets("Local_Sheet").Range("C216").value * 100
            Worksheets("New Input Mask").ComboBox5.value = Worksheets("Index").Range("AY2").value
            Worksheets("New Input Mask").Range("H41").value = Worksheets("Local_Sheet").Range("C217").value * 100
         Case "Buy Back Vehicles - Bank Branch"
                entryvalue = MsgBox(prompt:="Do you want to take over effective customer lending rate and flat fees from Local Sheet?", Buttons:=vbYesNo)
                If entryvalue = vbNo Then
                Exit Sub
                End If
            Worksheets("New Input Mask").Range("H54").value = Worksheets("Local_Sheet").Range("C218").value * 100
            Worksheets("New Input Mask").ComboBox5.value = Worksheets("Index").Range("AY2").value
            Worksheets("New Input Mask").Range("H41").value = Worksheets("Local_Sheet").Range("C219").value * 100
        Case "WFS Used Vehicles - Bank Branch"
                entryvalue = MsgBox(prompt:="Do you want to take over effective customer lending rate and flat fees from Local Sheet?", Buttons:=vbYesNo)
                If entryvalue = vbNo Then
                Exit Sub
                End If
            Worksheets("New Input Mask").Range("H54").value = Worksheets("Local_Sheet").Range("C220").value * 100
            Worksheets("New Input Mask").ComboBox5.value = Worksheets("Index").Range("AY2").value
            Worksheets("New Input Mask").Range("H41").value = Worksheets("Local_Sheet").Range("C221").value * 100
End Select
End Sub

Public Sub fee_transfer()

Select Case [Company]
Case "(AUT) Mercedes-Benz Financial Services Austria GmbH", "(AUT) Mercedes-Benz Financial Services Austria GmbH (Fleetmanagement)"
If Left([Opex_Segment], 5) = "MB PC" Then
Worksheets("New Input Mask").Range("H41") = [MBC_fee]
ElseIf Left([Opex_Segment], 5) = "Smart" Then
Worksheets("New Input Mask").Range("H41") = [smart_fee]
ElseIf Left([Opex_Segment], 10) = "Non DAI PC" Then
Worksheets("New Input Mask").Range("H41") = [Other_pc_fee]
ElseIf Left([Opex_Segment], 8) = "MB Truck" Then
Worksheets("New Input Mask").Range("H41") = [MB_TR_fee]
ElseIf Left([Opex_Segment], 4) = "Fuso" Then
Worksheets("New Input Mask").Range("H41") = [fu_fee]
ElseIf Left([Opex_Segment], 3) = "Bus" Then
Worksheets("New Input Mask").Range("H41") = [bu_fee]
ElseIf Left([Opex_Segment], 3) = "Van" Then
Worksheets("New Input Mask").Range("H41") = [mb_va_fee]
ElseIf Left([Opex_Segment], 10) = "Non DAI CV" Then
Worksheets("New Input Mask").Range("H41") = [other_cv_fee]
Else: Worksheets("New Input Mask").Range("H41") = [other_fee]
End If
Case "(AUT) Bank Austria GmbH"
If Left([Opex_Segment], 5) = "MB PC" Then
Worksheets("New Input Mask").Range("H41") = [MBC_fee_bank]
ElseIf Left([Opex_Segment], 5) = "Smart" Then
Worksheets("New Input Mask").Range("H41") = [smart_fee_bank]
ElseIf Left([Opex_Segment], 10) = "Non DAI PC" Then
Worksheets("New Input Mask").Range("H41") = [Other_pc_fee_bank]
ElseIf Left([Opex_Segment], 8) = "MB Truck" Then
Worksheets("New Input Mask").Range("H41") = [MB_TR_fee_bank]
ElseIf Left([Opex_Segment], 4) = "Fuso" Then
Worksheets("New Input Mask").Range("H41") = [fu_fee_bank]
ElseIf Left([Opex_Segment], 3) = "Bus" Then
Worksheets("New Input Mask").Range("H41") = [bu_fee_bank]
ElseIf Left([Opex_Segment], 3) = "Van" Then
Worksheets("New Input Mask").Range("H41") = [mb_va_fee_bank]
ElseIf Left([Opex_Segment], 10) = "Non DAI CV" Then
Worksheets("New Input Mask").Range("H41") = [other_cv_fee_bank]
Else: Worksheets("New Input Mask").Range("H41") = [other_fee_bank]
End If
End Select

Worksheets("New Input Mask").Select
Worksheets("New Input Mask").[e8].Activate
End Sub





--- Macro File: Frm_Target_ROR.frm ---
Attribute VB_Name = "Frm_Target_ROR"
Attribute VB_Base = "0{630A991B-30CE-421E-AE9D-FB1C5A4049F9}{04DD204A-EEF9-42C4-8C19-8CE32697637B}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private Sub UserForm_activate()
Label2.Caption = "Required " & Worksheets("New Input Mask").ComboBox3.value
CommandButton1.Caption = "Calculate " & Worksheets("New Input Mask").ComboBox3.value
End Sub

Private Sub CommandButton1_Click()

Dim value As Double


If (TextBox1.value <> "" And Not IsNumeric(TextBox1.value)) Or (TextBox1.value = "") Then
        MsgBox "Please enter a correct Target RORAC"
        Exit Sub
End If

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

If Application.UseSystemSeparators = True Then
    value = CDbl(TextBox1.value)
Else
    value = CDbl(Replace(TextBox1.value, strApp_Dec_Separator, strOS_Dec_Separator))
End If

[Target_RORAC] = value / 100
[Target_RORAC_Case] = "Yes"
[Target_Type] = "1"
Call unprotectInput

Call prcStartCalculation

If Application.UseSystemSeparators = True Then
    TextBox2.value = WorksheetFunction.Round([Target_Rate] * 100, 2)
Else
    TextBox2.value = Replace(WorksheetFunction.Round([Target_Rate] * 100, 2), strOS_Dec_Separator, strApp_Dec_Separator)
End If

Label5.Caption = [Target_Rate] * 100
[Target_RORAC_Case] = "No"

Call protectInput

End Sub

Private Sub CommandButton2_Click()

If (TextBox2.value = "") Then
        MsgBox "Please Calculate a Target Customer Rate"
        Exit Sub
End If

'Mexico case
If [Country_Short] = "MEX" And [tax_rate] > 0 And [TaxRefBase] = "Interest" And [TaxQuotationType] = 1 Then
    If [IDC] - [Subsidies] <> 0 Then
    Worksheets("New Input Mask").Range("H54").value = CDbl(Label5.Caption)
    Else
    Worksheets("New Input Mask").Range("H54").value = CDbl(Label5.Caption) / (1 + [tax_rate])
    End If
[Nom_CR_MCF] = CDbl(Label5.Caption) / (1 + [tax_rate])
Else
Worksheets("New Input Mask").Range("H54").value = CDbl(Label5.Caption)
[Nom_CR_MCF] = CDbl(Label5.Caption)
End If

Unload Me

Call unprotectInput

Call prcStartCalculation

[Target_RORAC_Case] = "No"

Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "Target RORAC was successfully calculated"
End Sub

Private Sub CommandButton3_Click()
[Target_RORAC_Case] = "No"
Unload Me
End Sub



--- Macro File: Frm_Language.frm ---
Attribute VB_Name = "Frm_Language"
Attribute VB_Base = "0{01C59D70-95ED-45B3-BF68-36E0E1C1EC8D}{1ECFEACA-31B5-465F-B1CD-D3853E6B691D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
'Form to enter, update or reset language settings for tool

'Sub to use "local" language entries
Private Sub CommandButton1_Click()

Application.EnableEvents = False
Call unprotectInput

'Check if a translation was entered for every input
For i = 1 To 3
    If Frm_Language("textbox" & Trim(Str(i)) & "b").value = "" Then
            Frm_Language("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a translation for """ & Frm_Language("label" & Trim(Str(i))).Caption & """"
            Frm_Language("textbox" & Trim(Str(i)) & "b").BackColor = RGB(63, 154, 206)
            Exit Sub
        End If
Next

For i = 4 To 78
    If Frm_Language("textbox" & Trim(Str(i)) & "b").value = "" Then
            Frm_Language("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 0, 0)
            MsgBox "Please enter a translation for """ & Frm_Language("textbox" & Trim(Str(i))).value & """"
            Frm_Language("textbox" & Trim(Str(i)) & "b").BackColor = RGB(255, 255, 255)
            Exit Sub
        End If
Next

intEntity_Row = fct_entity_data_position()

Dim wksinput As Worksheet
Dim wksPortfolio As Worksheet

Set wksinput = Sheets("New Input Mask")
Set wksPortfolio = Sheets("Portfolio")

'Setting of local words on input mask
With wksinput
    .Range("b2").value = TextBox1b.value
    .Range("b22").value = TextBox2b.value
    .Range("l22").value = TextBox3b.value
    .Range("c5").value = TextBox4b.value
    .Range("c8").value = TextBox5b.value
    .Range("c10").value = TextBox6b.value
    .Range("c12").value = TextBox18b.value
    .Range("c17").value = TextBox8b.value
    .Range("c19").value = TextBox9b.value
    .Range("h5").value = TextBox11b.value
    .Range("h8").value = TextBox12b.value
    .Range("h10").value = TextBox13b.value
    .Range("h17").value = TextBox15b.value
    .Range("h19").value = TextBox16b.value
    .Range("s17").value = TextBox20b.value
    .Range("q19").value = TextBox10b.value
    .Range("w17").value = TextBox21b.value
    .Range("j47").value = TextBox21b.value
    .Range("c25").value = TextBox24b.value
    .Range("c27").value = TextBox25b.value
    .Range("e27").value = TextBox26b.value
    .Range("c29").value = TextBox27b.value
    .Range("c31").value = TextBox28b.value
    .Range("c33").value = TextBox29b.value
    .Range("c35").value = TextBox30b.value
    .Range("c37").value = TextBox31b.value
    .Range("c39").value = TextBox32b.value
    .Range("c41").value = TextBox33b.value
    .Range("c43").value = TextBox34b.value
    .Range("c47").value = TextBox35b.value
    .Range("c49").value = TextBox36b.value
    .Range("g49").value = TextBox37b.value
    .Range("c52").value = TextBox38b.value
    .Range("g52").value = TextBox39b.value
    .Range("c54").value = TextBox40b.value
    .Range("c58").value = TextBox43b.value
    .Range("c60").value = TextBox44b.value
    .Range("c62").value = TextBox45b.value
    .Range("c61").value = TextBox73b.value
    .Range("c64").value = TextBox46b.value
    .Range("c66").value = TextBox47b.value
    .Range("c68").value = TextBox48b.value
    .Range("c70").value = TextBox49b.value
    .Range("n25").value = TextBox42b.value
    .Range("n27").value = TextBox52b.value
    .Range("n29").value = TextBox53b.value
    .Range("n31").value = TextBox54b.value
    .Range("n33").value = TextBox55b.value
    .Range("n35").value = TextBox56b.value
    .Range("q35").value = TextBox57b.value
    .Range("n37").value = TextBox58b.value
    .Range("n39").value = TextBox59b.value
    .Range("r39").value = TextBox60b.value
    .Range("n41").value = TextBox61b.value
    .Range("n43").value = TextBox62b.value
    .Range("n45").value = TextBox63b.value
    .Range("n47").value = TextBox64b.value
    .Range("n49").value = TextBox65b.value
    .Range("g58").value = TextBox69b.value
    .Range("g60").value = TextBox70b.value
    .Range("g62").value = TextBox71b.value
    .Range("g64").value = TextBox72b.value
End With

wksPortfolio.Unprotect Password:="Blattschutz"

'Setting of local words on portfolio sheet
With wksPortfolio
    .Range("c11").value = TextBox66b.value
    .Range("c12").value = TextBox24b.value
    .Range("c13").value = TextBox49b.value
    .Range("c14").value = TextBox35b.value & " (" & TextBox21.value & ")"
    .Range("c15").value = TextBox15b.value
    .Range("c16").value = TextBox68b.value
    .Range("c17").value = TextBox29b.value
    .Range("c22").value = TextBox42b.value
    .Range("c23").value = TextBox62b.value
    .Range("c24").value = TextBox53b.value
    .Range("c25").value = TextBox54b.value
    .Range("c26").value = TextBox55b.value
    .Range("c27").value = TextBox56b.value
    .Range("c28").value = TextBox58b.value
    .Range("c29").value = TextBox59b.value
    .Range("c30").value = TextBox61b.value
    .Range("c31").value = TextBox62b.value
    .Range("c32").value = TextBox63b.value
    .Range("c33").value = TextBox64b.value
    .Range("c35").value = TextBox65b.value
End With
wksPortfolio.Protect Password:="Blattschutz"

'Setting of language terms for textbox lists
Worksheets("Index").Range("aa2").value = TextBox10b.value
Worksheets("Index").Range("aa3").value = TextBox17b.value
Worksheets("Index").Range("aa4").value = "=""" & TextBox22b.value & " " & """ & Deal_Currency"
Worksheets("New Input Mask").Range("Q19").value = Worksheets("Index").Range("Aa4").value
Worksheets("Index").Range("aa5").value = TextBox23b.value
Worksheets("Index").Range("aa6").value = TextBox19b.value

Worksheets("Index").Range("r3").value = TextBox41b.value
Worksheets("Index").Range("r2").value = TextBox42b.value
Worksheets("New Input Mask").ComboBox3.value = Worksheets("Index").Range("r2").value

Worksheets("Index").Range("ao2").value = TextBox50b.value
Worksheets("Index").Range("ao3").value = TextBox51b.value
Worksheets("New Input Mask").Range("E70").value = TextBox50b.value

Worksheets("Index").Range("BE2").value = TextBox74b.value
Worksheets("Index").Range("BE3").value = TextBox75b.value
Worksheets("New Input Mask").Range("g33").value = Worksheets("New Input Mask").Range("d27").value

Worksheets("Index").Range("Z2").value = TextBox76b.value
Worksheets("Index").Range("Z3").value = TextBox77b.value
Worksheets("Index").Range("Z4").value = TextBox78b.value
Worksheets("New Input Mask").Range("g45").value = Worksheets("New Input Mask").Range("d27").value
Worksheets("New Input Mask").Range("g37").value = Worksheets("New Input Mask").Range("d27").value

Worksheets("Index").Range("AY2").value = TextBox79b.value
Worksheets("Index").Range("AY3").value = TextBox80b.value
Worksheets("Index").Range("AY4").value = TextBox81b.value
Worksheets("New Input Mask").Range("f41").value = TextBox79b.value

'storage of language settings on data entities sheet for specific entity
For i = 1 To 81
    Worksheets("Data_Entities").Cells(intEntity_Row + i, 62).value = Frm_Language("textbox" & Trim(Str(i)) & "b").value
Next

Application.EnableEvents = True

Unload Me
Call protectInput

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

'Use English Language

Private Sub CommandButton3_Click()

Dim wksinput As Worksheet
Dim wksPortfolio As Worksheet

Set wksinput = Sheets("New Input Mask")
Set wksPortfolio = Sheets("Portfolio")

Application.EnableEvents = False

Call unprotectInput

'Setting of english terms on input sheet
With wksinput
    .Range("b2").value = TextBox1.value
    .Range("b22").value = TextBox2.value
    .Range("l22").value = TextBox3.value
    .Range("c5").value = TextBox4.value
    .Range("c8").value = TextBox5.value
    .Range("c10").value = TextBox6.value
    .Range("c12").value = TextBox18.value
    .Range("c17").value = TextBox8.value
    .Range("c19").value = TextBox9.value
    .Range("h5").value = TextBox11.value
    .Range("h8").value = TextBox12.value
    .Range("h10").value = TextBox13.value
    .Range("h17").value = TextBox15.value
    .Range("h19").value = TextBox16.value
    .Range("s17").value = TextBox20.value
    .Range("q19").value = TextBox10.value
    .Range("w17").value = TextBox21.value
    .Range("j47").value = TextBox21.value
    .Range("c25").value = TextBox24.value
    .Range("c27").value = TextBox25.value
    .Range("e27").value = TextBox26.value
    .Range("c29").value = TextBox27.value
    .Range("c31").value = TextBox28.value
    .Range("c33").value = TextBox29.value
    .Range("c35").value = TextBox30.value
    .Range("c37").value = TextBox31.value
    .Range("c39").value = TextBox32.value
    .Range("c41").value = TextBox33.value
    .Range("c43").value = TextBox34.value
    .Range("c47").value = TextBox35.value
    .Range("c49").value = TextBox36.value
    .Range("g49").value = TextBox37.value
    .Range("c52").value = TextBox38.value
    .Range("g52").value = TextBox39.value
    .Range("c54").value = TextBox40.value
    .Range("c58").value = TextBox43.value
    .Range("c60").value = TextBox44.value
    .Range("c61").value = TextBox73.value
    .Range("c62").value = TextBox45.value
    .Range("c64").value = TextBox46.value
    .Range("c66").value = TextBox47.value
    .Range("c68").value = TextBox48.value
    .Range("c70").value = TextBox49.value
    .Range("n25").value = TextBox42.value
    .Range("n27").value = TextBox52.value
    .Range("n29").value = TextBox53.value
    .Range("n31").value = TextBox54.value
    .Range("n33").value = TextBox55.value
    .Range("n35").value = TextBox56.value
    .Range("q35").value = TextBox57.value
    .Range("n37").value = TextBox58.value
    .Range("n39").value = TextBox59.value
    .Range("r39").value = TextBox60.value
    .Range("n41").value = TextBox61.value
    .Range("n43").value = TextBox62.value
    .Range("n45").value = TextBox63.value
    .Range("n47").value = TextBox64.value
    .Range("n49").value = TextBox65.value
    .Range("g58").value = TextBox69.value
    .Range("g60").value = TextBox70.value
    .Range("g62").value = TextBox71.value
    .Range("g64").value = TextBox72.value
End With

'Setting of english terms on portfolio sheet
wksPortfolio.Unprotect Password:="Blattschutz"
With wksPortfolio
    .Range("c11").value = TextBox66.value
    .Range("c12").value = TextBox24.value
    .Range("c13").value = TextBox49.value
    .Range("c14").value = "Credit Term in Months"
    .Range("c15").value = TextBox15.value
    .Range("c16").value = TextBox68.value
    .Range("c17").value = TextBox29.value
    .Range("c22").value = TextBox42.value
    .Range("c23").value = TextBox62.value
    .Range("c24").value = TextBox53.value
    .Range("c25").value = TextBox54.value
    .Range("c26").value = TextBox55.value
    .Range("c27").value = TextBox56.value
    .Range("c28").value = TextBox58.value
    .Range("c29").value = TextBox59.value
    .Range("c30").value = TextBox61.value
    .Range("c31").value = TextBox62.value
    .Range("c32").value = TextBox63.value
    .Range("c33").value = TextBox64.value
    .Range("c35").value = TextBox65.value
End With

'Setting of language terms for textbox lists
wksPortfolio.Protect Password:="Blattschutz"

Worksheets("Index").Range("aa2").value = TextBox10.value
Worksheets("Index").Range("aa3").value = TextBox17.value
Worksheets("Index").Range("aa4").value = "=""" & TextBox22.value & " " & """ & Deal_Currency"
Worksheets("New Input Mask").Range("Q19").value = Worksheets("Index").Range("Aa4").value
Worksheets("Index").Range("aa5").value = TextBox23.value
Worksheets("Index").Range("aa6").value = TextBox19.value

Worksheets("Index").Range("r3").value = TextBox41.value
Worksheets("Index").Range("r2").value = TextBox42.value
Worksheets("New Input Mask").ComboBox3.value = Worksheets("Index").Range("r2").value

Worksheets("Index").Range("BE2").value = TextBox74.value
Worksheets("Index").Range("BE3").value = TextBox75.value
Worksheets("New Input Mask").Range("g33").value = Worksheets("New Input Mask").Range("d27").value

Worksheets("Index").Range("ao2").value = TextBox50.value
Worksheets("Index").Range("ao3").value = TextBox51.value
Worksheets("New Input Mask").Range("E70").value = TextBox50.value

Worksheets("Index").Range("Z2").value = TextBox76.value
Worksheets("Index").Range("Z3").value = TextBox77.value
Worksheets("Index").Range("Z4").value = TextBox78.value
Worksheets("New Input Mask").Range("g45").value = Worksheets("New Input Mask").Range("d27").value
Worksheets("New Input Mask").Range("g37").value = Worksheets("New Input Mask").Range("d27").value

Worksheets("Index").Range("AY2").value = TextBox79.value
Worksheets("Index").Range("AY3").value = TextBox80.value
Worksheets("Index").Range("AY4").value = TextBox81.value
Worksheets("New Input Mask").Range("f41").value = TextBox79.value

Application.EnableEvents = True


Unload Me
Call protectInput

End Sub

'sub to prefill textboxes with local terms when activating form
Private Sub UserForm_activate()

Dim intEntity_Row As Integer

intEntity_Row = fct_entity_data_position()

For i = 1 To 81
    Frm_Language("textbox" & Trim(Str(i)) & "b").value = Worksheets("Data_Entities").Cells(intEntity_Row + i, 62).value
Next

End Sub


--- Macro File: Modul1.bas ---
Attribute VB_Name = "Modul1"
Sub Makro1()
Attribute Makro1.VB_Description = "Makro am 16.10.2009 von SBaller aufgezeichnet"
Attribute Makro1.VB_ProcData.VB_Invoke_Func = " \n14"
'
' Makro1 Makro
' Makro am 16.10.2009 von SBaller aufgezeichnet
'
Application.EnableEvents = True
'
End Sub
Sub Makro2()
'
' Makro1 Makro
' Makro am 16.10.2009 von SBaller aufgezeichnet
'
Application.EnableEvents = False
'
End Sub


Sub PrepareForRelease()

ActiveWorkbook.Unprotect ("Blattschutz")
If Worksheets("Index").Visible = xlVeryHidden Or Worksheets("Index").Visible = xlHidden Then
    Worksheets("Index").Visible = xlSheetVisible
    Worksheets("Index").Activate
    ActiveSheet.Unprotect ("Blattschutz")
    Worksheets("Index").Range("Initialized").value = "No"
    Worksheets("Index").Visible = xlVeryHidden
Else
    ActiveSheet.Unprotect ("Blattschutz")
    Worksheets("Index").Activate
    Worksheets("Index").Range("Initialized").value = "No"
    Worksheets("Index").Visible = xlVeryHidden
End If

  If Worksheets("Initialize").Visible = xlVeryHidden Or Worksheets("Initialize").Visible = xlHidden Then
    Worksheets("Initialize").Visible = xlSheetVisible
    Worksheets("Initialize").Select
  End If
  If Worksheets("Initialize").Visible = xlVeryHidden Or Worksheets("Initialize").Visible = xlHidden Then
    Worksheets("Initialize").Visible = xlSheetVisible
    Worksheets("Initialize").Select
  End If
  
  ActiveWindow.DisplayHeadings = False
 End Sub


--- Macro File: Tabelle1.cls ---
Attribute VB_Name = "Tabelle1"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Frm_DiscLP.frm ---
Attribute VB_Name = "Frm_DiscLP"
Attribute VB_Base = "0{89B28CCC-13C7-445C-8F94-D9797B8B2963}{420F92BD-3454-4B7D-841C-A4F36C17BD82}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Public intEntity_Row As Integer
Private strOS_Dec_Separator As String
Private strApp_Dec_Separator As String


'Sub to calculate discount and copy value to input mask
Private Sub CommandButton1_Click()
Dim discount As Double

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

varTB1 = Replace(TextBox1.value, Application.DecimalSeparator, strOS_Dec_Separator)

'Depending on the setting for system separators the discount will be calculated and copied to the input mask
If Application.UseSystemSeparators = True Then
    'Check if absolute or percentage discount was added
    If OptionButton1.value = True Then
        'Validation of entered values
        If IsNumeric(TextBox1.value) And TextBox1.value > 0 And TextBox1.value <= 100 Then
            discount = Worksheets("New Input Mask").Range("e17").value * TextBox1.value / 100
            Worksheets("New Input Mask").Range("h29").value = Application.Round(Worksheets("New Input Mask").Range("e17").value - discount, 0)
            Unload Me
            Exit Sub
        Else
            MsgBox ("Please enter a valid discount")
        End If
    Else
        If IsNumeric(TextBox1.value) And TextBox1.value > 1 And CDbl(TextBox1.value) < Worksheets("New Input Mask").Range("e17").value Then
            Worksheets("New Input Mask").Range("h29").value = Worksheets("New Input Mask").Range("e17").value - CDbl(TextBox1.value)
            Unload Me
            Exit Sub
        Else
            MsgBox ("Please enter a valid discount")
        End If
    End If
Else
    'Check if absolute or percentage discount was added
    If OptionButton1.value = True Then
        If IsNumeric(TextBox1.value) And varTB1 > 0 And varTB1 <= 100 Then
            discount = Worksheets("New Input Mask").Range("e17").value * varTB1 / 100
            Worksheets("New Input Mask").Range("h29").value = Application.Round(Worksheets("New Input Mask").Range("e17").value - discount, 0)
            Unload Me
            Exit Sub
        Else
            MsgBox ("Please enter a valid discount")
        End If
    Else
        If IsNumeric(TextBox1.value) And varTB1 > 1 And CDbl(varTB1) < CDbl(Worksheets("New Input Mask").Range("e17").value) Then
            Worksheets("New Input Mask").Range("h29").value = Worksheets("New Input Mask").Range("e17").value - varTB1
            Unload Me
            Exit Sub
        Else
            MsgBox ("Please enter a valid discount")
        End If
    End If
End If

End Sub

'Sub to close form
Private Sub CommandButton2_Click()
Unload Me
End Sub

'Sub to set currency to deal currency when activating form
Private Sub UserForm_activate()
OptionButton2.Caption = Left([Deal_Currency], 3)
End Sub


--- Macro File: Frm_Target_Calc.frm ---
Attribute VB_Name = "Frm_Target_Calc"
Attribute VB_Base = "0{40844A32-3A3A-4BFA-9AFF-0770A5078BEE}{517F2C44-C10C-42C8-BAC9-8959E16FF0D6}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private Sub CommandButton1_Click()
Unload Me
Frm_Target_ROR.Show
End Sub

Private Sub CommandButton3_Click()
Unload Me
Frm_Target_Red.Show
End Sub

Private Sub CommandButton4_Click()
If Worksheets("New Input Mask").ComboBox3.value = Worksheets("Index").Range("r3").value Then
    MsgBox "Target function not available for " & Worksheets("Index").Range("r3").value & ". Please change to " & Worksheets("Index").Range("r2").value & "."
    Exit Sub
Else
    Unload Me
    Frm_Target_Sub.Show
End If
End Sub

Private Sub CommandButton9_Click()
Unload Me
Frm_Target_Instal.Show
End Sub

Private Sub CommandButton7_Click()
Unload Me
End Sub


--- Macro File: Frm_Benchmark.frm ---
Attribute VB_Name = "Frm_Benchmark"
Attribute VB_Base = "0{8C7C3D9E-EFA2-4883-8E1E-28D09879C977}{E861D9CD-98E4-41A3-925F-2D55188E404E}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private strOS_Dec_Separator As String
Private strApp_Dec_Separator As String

'in case of changing the BM resptive interest rate will be displayed in textbox3
Private Sub ComboBox1_Change()

'determine system and application decimal separator
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

Dim i As Integer
Dim found As Boolean
Dim dblTB1 As Double
Dim dblTB3 As Double

'search for respective interest rate of selected bm and display at textbox3 under consideration of correct decimal separator
i = 1
While found = False And i < 13
If ComboBox1.text = Worksheets("Index").Cells(183 + i, 4).value Then
    If Application.UseSystemSeparators = True Then
        TextBox3.text = Worksheets("Index").Cells(183 + i, 5)
    Else
        TextBox3.text = Replace(Worksheets("Index").Cells(183 + i, 5).value, strOS_Dec_Separator, Application.DecimalSeparator)
    End If
    gefunden = True
End If
i = i + 1
Wend

'calculate total customer rate by sum up bm rate and spread
If Application.UseSystemSeparators = True Then
    If TextBox1.text <> "" Then
    TextBox2.value = CDbl(TextBox1.text) + CDbl(TextBox3.text)
    Else
    TextBox2.text = CDbl(TextBox3.text)
    End If
Else
    dblTB3 = Replace(TextBox3.text, Application.DecimalSeparator, strOS_Dec_Separator)
    If TextBox1.text <> "" Then
        dblTB1 = Replace(TextBox1.text, Application.DecimalSeparator, strOS_Dec_Separator)
        TextBox2.text = Replace(CDbl(dblTB1) + CDbl(dblTB3), strOS_Dec_Separator, Application.DecimalSeparator)
    Else
        TextBox2.text = Replace(dblTB3, strOS_Dec_Separator, Application.DecimalSeparator)
    End If
End If

End Sub

'copy customer rate to input mask and bm + rate to index sheet that it can be displayed on calculation schema
Private Sub CommandButton1_Click()
 
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

If Application.UseSystemSeparators = True Then
    Worksheets("New Input Mask").Range("h54").value = Application.Round(TextBox2.text / 1, 2)
    Worksheets("Index").Range("B262").value = ComboBox1.text
    Worksheets("Index").Range("B263").value = CDbl(TextBox3.text)
Else
    Worksheets("New Input Mask").Range("h54").value = Application.Round(Replace(TextBox2.text, strApp_Dec_Separator, strOS_Dec_Separator) / 1, 2)
    Worksheets("Index").Range("B262").value = ComboBox1.text
    Worksheets("Index").Range("B263").value = CDbl(Replace(TextBox3.text, strApp_Dec_Separator, strOS_Dec_Separator))
End If
Unload Me
End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

'sub to check if entered spread is valid
Private Sub TextBox1_Change()

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'check if entered spread is a numeric value and below 20 --> please consider the 2 streams depending on the fact if systemseparators are used
'if check is successful than sum up of spread + bm rate
If Application.UseSystemSeparators = True Then
    If TextBox1.value = "" Then
    ElseIf Not IsNumeric(TextBox1.value) Then
        MsgBox "Please enter a numeric value"
        TextBox1.text = ""
        TextBox2.text = CDbl(TextBox3.text)
    ElseIf (TextBox1.text) > 20 Then
        MsgBox "Please enter a lower Spread"
        TextBox1.value = ""
        TextBox2.text = CDbl(TextBox3.text)
    Else
        TextBox2.text = CDbl(TextBox1.text) + CDbl(TextBox3.text)
    End If
Else
    varTB1 = Replace(TextBox1.value, Application.DecimalSeparator, strOS_Dec_Separator)
    varTB2 = Replace(TextBox2.value, Application.DecimalSeparator, strOS_Dec_Separator)
    varTB3 = Replace(TextBox3.value, Application.DecimalSeparator, strOS_Dec_Separator)
    If TextBox1.value = "" Then
    ElseIf Not IsNumeric(varTB1) Then
        MsgBox "Please enter a numeric value"
        TextBox1.value = ""
        TextBox2.value = Replace(varTB3, strOS_Dec_Separator, Application.DecimalSeparator)
    ElseIf (varTB1) > 20 Then
        MsgBox "Please enter a lower Spread"
        TextBox1.value = ""
        TextBox2.value = Replace(varTB3, strOS_Dec_Separator, Application.DecimalSeparator)
    Else
        TextBox2.value = Replace(CDbl(varTB3) + CDbl(varTB1), strOS_Dec_Separator, Application.DecimalSeparator)
    End If
End If
End Sub

'setting initiale value of to first value of bm list
Private Sub UserForm_activate()
ComboBox1.ListIndex = 0
End Sub


--- Macro File: Frm_Password.frm ---
Attribute VB_Name = "Frm_Password"
Attribute VB_Base = "0{56DF2739-83B9-4732-890F-D5E0D00150CB}{30C89886-70F2-4798-88C6-FBA24A981C4A}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private Sub CommandButton1_Click()
Dim pw As String

pw = TextBox1.text
Dim t As Variant

If pw <> "parameter" Then
   MsgBox ("Wrong Password, please try again.")
   TextBox1.text = ""
Else
    Unload Me
    Frm_Main.Show
End If

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub



--- Macro File: Tabelle3.cls ---
Attribute VB_Name = "Tabelle3"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Tabelle4.cls ---
Attribute VB_Name = "Tabelle4"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "CommandButton1, 12, 0, MSForms, CommandButton"
Private Sub CommandButton1_Click()
Worksheets("New Input Mask").Activate
Application.EnableEvents = True
End Sub


Private Sub Worksheet_Activate()

Application.ScreenUpdating = False
Dim i As Integer
For i = 4 To 123
    If Range("F" & i).value = 0 And Range("R" & i).value <> 0 Then
        Range("R" & i).value = 0
    End If
Next i
Application.ScreenUpdating = True
ActiveSheet.Range("R4").Select
End Sub


--- Macro File: Frm_Comments.frm ---
Attribute VB_Name = "Frm_Comments"
Attribute VB_Base = "0{B63D9D80-AC31-4DF9-AE21-6BEAAC35F3EF}{182B8A86-FC81-4922-B4D1-EBC48F4248FB}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private Sub CommandButton1_Click()
Call unprotectInput
Worksheets("New Input Mask").Range("L58").value = Replace(TextBox1.text, vbCrLf, vbLf)
Worksheets("New Input Mask").Range("L74").value = Replace(TextBox1.text, vbCrLf, vbLf)
If TextBox1.text = "" Then Application.CalculateFullRebuild
Call protectInput
Unload Me
End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

Private Sub UserForm_activate()
TextBox1.text = Worksheets("New Input Mask").Range("L58").value
TextBox1.text = Worksheets("New Input Mask").Range("L74").value
End Sub


--- Macro File: Tabelle10.cls ---
Attribute VB_Name = "Tabelle10"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "ComboBox1, 5, 7, MSForms, ComboBox"
Attribute VB_Control = "ComboBox2, 6, 8, MSForms, ComboBox"
Attribute VB_Control = "ComboBox3, 7, 9, MSForms, ComboBox"
Attribute VB_Control = "ComboBox4, 9, 10, MSForms, ComboBox"
Attribute VB_Control = "CommandButton6, 10, 11, MSForms, CommandButton"
Attribute VB_Control = "CommandButton7, 11, 12, MSForms, CommandButton"
Attribute VB_Control = "CommandButton8, 12, 13, MSForms, CommandButton"
Attribute VB_Control = "CommandButton11, 36, 14, MSForms, CommandButton"
Attribute VB_Control = "CommandButton12, 48, 15, MSForms, CommandButton"
Attribute VB_Control = "CommandButton13, 49, 16, MSForms, CommandButton"
Attribute VB_Control = "CommandButton14, 50, 17, MSForms, CommandButton"
Attribute VB_Control = "SpinButton1, 59, 18, MSForms, SpinButton"
Attribute VB_Control = "CommandButton15, 66, 19, MSForms, CommandButton"


--- Macro File: Frm_MCF.frm ---
Attribute VB_Name = "Frm_MCF"
Attribute VB_Base = "0{5CCEF8CB-9CE6-4EE5-8CE6-12529E3B6883}{8B799B53-F1BC-4A06-8FEA-2E8A89BABB0F}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
'Sub to validate if end date for 3rd stream can be entered or not
Private Sub ComboBox10_Change()
If bol_ChangeEvent = False Then
    Exit Sub
End If

'Check if start date for 3rd stream was entered
If ComboBox9.value = "" And ComboBox9.Visible = True Then
    bol_ChangeEvent = False
    ComboBox10.value = ""
    MsgBox "Please enter a start date first."
    bol_ChangeEvent = True
Else
    'check if end date for 3rd stream is later than start date
    If ComboBox10.value <> "" Then
        ComboBox10.value = Format(ComboBox10.value, "Short Date")
        If CDate(ComboBox9.value) > CDate(ComboBox10.value) Then
            MsgBox "Please enter an end date that is not earlier than start date  "
            ComboBox10.value = ""
        End If
    End If
End If
End Sub

'Sub to validate if end date for 1st stream can be entered or not
Private Sub ComboBox2_Change()
If bol_ChangeEvent = False Then
    Exit Sub
End If
'Check if start date for 1st stream was entered
If ComboBox1.value = "" Then
    bol_ChangeEvent = False
    ComboBox2.value = ""
    MsgBox "Please enter a start date first."
    bol_ChangeEvent = True
Else
    'check if end date for 1st stream is later than start date
    If ComboBox2.value <> "" Then
        ComboBox2.value = Format(ComboBox2.value, "Short Date")
        If CDate(ComboBox1.value) > CDate(ComboBox2.value) Then
             MsgBox "Please enter an end date that is not earlier than start date  "
             ComboBox2.value = ""
         End If
    End If
End If
End Sub

Private Sub ComboBox1_Change()
Me.ComboBox1.value = Format(Me.ComboBox1.value, "Short Date")
End Sub

'Sub to validate if start date for 2nd stream is correct
Private Sub ComboBox5_Change()

Me.ComboBox5.value = Format(Me.ComboBox5.value, "Short Date")
'Ckheck if start date of second stream is later than end date of first stream
If ComboBox5.value <> "" Then
    If CDate(ComboBox5.value) <= CDate(ComboBox2.value) Then
        Me.ComboBox5.value = ""
        MsgBox "Start date cannot be before end date of previous payment stream"
    End If
End If
End Sub

'Sub to validate if end date for 2nd stream can be entered or not
Private Sub ComboBox6_Change()
If bol_ChangeEvent = False Then
    Exit Sub
End If

'Check if start date for 2nd stream was entered
If ComboBox5.value = "" And ComboBox5.Visible = True Then
    bol_ChangeEvent = False
    ComboBox6.value = ""
    MsgBox "Please enter a start date first."
    bol_ChangeEvent = True
Else
    'check if end date for 2nd stream is later than start date
    If ComboBox6.value <> "" Then
        ComboBox6.value = Format(ComboBox6.value, "Short Date")
        If CDate(ComboBox5.value) > CDate(ComboBox6.value) Then
            MsgBox "Please enter an end date that is not earlier than start date  "
            ComboBox6.value = ""
        End If
    End If
End If
End Sub

'Sub to validate if start date for 4th stream is correct
Private Sub ComboBox13_Change()
Me.ComboBox13.value = Format(Me.ComboBox13.value, "Short Date")
If ComboBox13.value <> "" Then
    If CDate(ComboBox13.value) <= CDate(ComboBox10.value) Then
        Me.ComboBox13.value = ""
        MsgBox "Start date cannot be before end date of previous payment stream"
    End If
End If
End Sub
'Sub to validate if end date for 4th stream can be entered or not
Private Sub ComboBox14_change()
If bol_ChangeEvent = False Then
    Exit Sub
End If

'Check if start date for 4th stream was entered
If ComboBox13.value = "" And ComboBox13.Visible = True Then
    bol_ChangeEvent = False
    ComboBox14.value = ""
    MsgBox "Please enter a start date first."
    bol_ChangeEvent = True
Else
     'check if end date for 4th stream is later than start date
    If ComboBox14.value <> "" Then
        ComboBox14.value = Format(ComboBox14.value, "Short Date")
        If ComboBox14.value <> "" Then
            If CDate(ComboBox13.value) > CDate(ComboBox14.value) Then
                MsgBox "Please enter an end date that is not earlier than start date  "
                ComboBox14.value = ""
            End If
        End If
    End If
End If
End Sub

'Sub to copy inputs from MCF-form to sheet
Private Sub CommandButton1_Click()

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

With Application
        .Calculate
        .Calculation = xlManual
        .MaxChange = 0.001
        .ScreenUpdating = False
End With

'Check if entered irregular payments are valid numeric amounts
For i = 1 To 13 Step 4
    If Frm_MCF("Combobox" & Trim(Str(i))).Visible = True And Frm_MCF("Combobox" & Trim(Str(i))).value <> "" And Frm_MCF("Combobox" & Trim(Str(i + 1))).value <> "" Then
        If Application.UseSystemSeparators = True Then
            amount = Frm_MCF("Textbox" & Trim(Str(i))).value
        Else
            amount = Replace(Frm_MCF("Textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If
        
        If (Not IsNumeric(Frm_MCF("Textbox" & Trim(Str(i))).value)) And Frm_MCF("Textbox" & Trim(Str(i))).value <> "" Then
                Frm_MCF("Textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
                MsgBox "Please enter a Irregular Payment amount"
                Frm_MCF("Textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
                With Application
                    .Calculation = xlAutomatic
                    .MaxChange = 0.001
                    .ScreenUpdating = True
                End With
                Exit Sub
        End If
    End If
Next

'Sub to copy inputs from form to MCF sheet
For i = 1 To 13 Step 4
    If Frm_MCF("Combobox" & Trim(Str(i))).Visible = True And Frm_MCF("Combobox" & Trim(Str(i))).value <> "" And Frm_MCF("Combobox" & Trim(Str(i + 1))).value <> "" Then
        For j = 1 To 121
            'Check if date on MCF sheet is affected from entered adjust on form
            If (CDate(Frm_MCF("Combobox" & Trim(Str(i))).value) <= Worksheets("Manual_Cash_Flows").Cells(j + 1, 2).value) And (CDate(Frm_MCF("Combobox" & Trim(Str(i + 1))).value) >= Worksheets("Manual_Cash_Flows").Cells(j + 1, 2).value) Then
                'Setting of (hidden) column A
                If Frm_MCF("Combobox" & Trim(Str(i + 2))).value = "Yes" Then
                    Worksheets("Manual_Cash_Flows").Cells(j + 1, 1).value = "Yes"
                    Else
                    Worksheets("Manual_Cash_Flows").Cells(j + 1, 1).value = "No"
                End If
                'Setting of payment type
                Worksheets("Manual_Cash_Flows").Cells(j + 1, 5).value = Frm_MCF("Combobox" & Trim(Str(i + 3))).value
                'Setting of irregular payment type
                If Frm_MCF("Textbox" & Trim(Str(i))).value = "" Then
                    If j <> 1 Then
                        Worksheets("Manual_Cash_Flows").Cells(j + 1, 3).value = 0
                    End If
                Else
                    If j = 1 Then
                        If Application.UseSystemSeparators = True Then
                            Worksheets("Manual_Cash_Flows").Cells(j + 1, 3).value = Worksheets("Manual_Cash_Flows").Cells(j + 1, 3).value + CDbl(Frm_MCF("Textbox" & Trim(Str(i))).value)
                        Else
                            Worksheets("Manual_Cash_Flows").Cells(j + 1, 3).value = Worksheets("Manual_Cash_Flows").Cells(j + 1, 3).value + CDbl(Replace(Frm_MCF("Textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator))
                        End If
                    Else
                        If Application.UseSystemSeparators = True Then
                            Worksheets("Manual_Cash_Flows").Cells(j + 1, 3).value = CDbl(Frm_MCF("Textbox" & Trim(Str(i))).value)
                        Else
                            Worksheets("Manual_Cash_Flows").Cells(j + 1, 3).value = CDbl(Replace(Frm_MCF("Textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator))
                        End If
                    End If
                End If
            End If
        Next
    End If
Next

With Application
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With
    
Unload Me
End Sub

'Sub to validate if start date for 3rd stream is correct
Private Sub ComboBox9_Change()
Me.ComboBox9.value = Format(Me.ComboBox9.value, "Short Date")
If ComboBox9.value <> "" Then
    If CDate(ComboBox9.value) <= CDate(ComboBox6.value) Then
        Me.ComboBox9.value = ""
        MsgBox "Start date cannot be before end date of previous payment stream"
    End If
End If
End Sub


Private Sub CommandButton2_Click()
Unload Me
End Sub

'Sub to delete last stream
Private Sub CommandButton4_Click()

If int_Anzstreams = 1 Then
    Exit Sub
Else
int_Anzstreams = int_Anzstreams - 1
Frm_MCF("textbox" & Trim(Str(int_Anzstreams * 4 + 1))).Visible = False
Frm_MCF("label" & Trim(Str(int_Anzstreams * 4 + 1))).Visible = False

For i = int_Anzstreams * 4 + 1 To int_Anzstreams * 4 + 4
    Frm_MCF("Combobox" & Trim(Str(i))).Visible = False
Next

End If
End Sub

'Sub to add additional payment stream
Private Sub CommandButton3_Click()

If int_Anzstreams = 4 Then
    Exit Sub
End If

'Check if previous payment stream was entered correctly as a precondition to enter a new one
If Frm_MCF("combobox" & Trim(Str(int_Anzstreams * 4 - 3))).value = "" Or Frm_MCF("combobox" & Trim(Str(int_Anzstreams * 4 - 2))).value = "" Then
    MsgBox "Please enter previous payment stream correctly first."
    Exit Sub
End If

Frm_MCF("textbox" & Trim(Str(int_Anzstreams * 4 + 1))).Visible = True
Frm_MCF("label" & Trim(Str(int_Anzstreams * 4 + 1))).Visible = True

For i = int_Anzstreams * 4 + 1 To int_Anzstreams * 4 + 4
    Frm_MCF("Combobox" & Trim(Str(i))).Visible = True
Next
For i = int_Anzstreams * 4 + 1 To int_Anzstreams * 4 + 2
    bol_ChangeEvent = False
    Frm_MCF("Combobox" & Trim(Str(i))).value = ""
    bol_ChangeEvent = True
Next
int_Anzstreams = int_Anzstreams + 1

End Sub

Private Sub UserForm_activate()
'variable to count number of streams
int_Anzstreams = 1

Label1.Caption = [Deal_Currency]
Label5.Caption = [Deal_Currency]
Label9.Caption = [Deal_Currency]
Label13.Caption = [Deal_Currency]

Label5.Visible = False
Label9.Visible = False
Label13.Visible = False

Frm_MCF("textbox" & Trim(Str(5))).Visible = False
Frm_MCF("textbox" & Trim(Str(9))).Visible = False
Frm_MCF("textbox" & Trim(Str(13))).Visible = False
For i = 5 To 16
    Frm_MCF("Combobox" & Trim(Str(i))).Visible = False
Next

ComboBox1.value = Format(Worksheets("Manual_Cash_Flows").Range("B2").value, "Short Date")
ComboBox2.value = Format(Worksheets("Manual_Cash_Flows").Range("B2").value, "Short Date")
bol_ChangeEvent = True
End Sub



--- Macro File: Tabelle5.cls ---
Attribute VB_Name = "Tabelle5"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "CommandButton1, 10, 1, MSForms, CommandButton"
Attribute VB_Control = "CommandButton2, 42, 2, MSForms, CommandButton"
Attribute VB_Control = "CommandButton3, 43, 3, MSForms, CommandButton"
Attribute VB_Control = "CommandButton4, 56, 4, MSForms, CommandButton"
Private Sub CommandButton1_Click()

'Set Flag for Manual cash Flow and Customer rate if it was changed on MCF-Sheet
Worksheets("Index").Range("Manual_CF_Flag") = 1
Worksheets("New Input Mask").Range("H54").value = [Nom_CR_MCF]

'Calculation of new RORAC with new cash flow structure and if set new Customer Rate
Call unprotectInput
Call prcStartCalculation
Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "RORAC was successfully calculated"

Worksheets("New Input Mask").Activate
End Sub

'Sub to reset entered MCF to initiale values
Private Sub CommandButton2_Click()
Application.ScreenUpdating = False
Application.Calculation = xlCalculationManual

Dim i As Integer
i = 3

Do While Worksheets("Manual_Cash_Flows").Cells(i, 3).value <> ""
    'Reset of "Any Payment" Column"
    Worksheets("Manual_Cash_Flows").Cells(i, 1).value = "No"
    
    'Rest of irregular Payments
    If Worksheets("Manual_Cash_Flows").Cells(i, 3).value <> 0 And Worksheets("Manual_Cash_Flows").Cells(i + 1, 3).value <> "" Then
        Worksheets("Manual_Cash_Flows").Cells(i, 3).value = 0
    End If
    
    'Reset of regular payment column--> in case of in advance payment mode the last entry is "no regular payment"
    If Worksheets("Manual_Cash_Flows").Cells(i + 1, 3).value = "" And [Payment_Mode] = "In Advance" Then
        Worksheets("Manual_Cash_Flows").Cells(i, 5).value = "No Regular Payment"
    Else
        Worksheets("Manual_Cash_Flows").Cells(i, 5).value = "Principal and Interest"
    End If
    i = i + 1
Loop
Application.ScreenUpdating = True
Application.Calculation = xlCalculationAutomatic

End Sub

'sub to open form to enter manual cash flow
Private Sub CommandButton3_Click()

Frm_MCF.Show

End Sub

'sub to recalculate RORAC without leaving the sheet
Private Sub CommandButton4_Click()

Worksheets("New Input Mask").Range("H54").value = [Nom_CR_MCF]

Call unprotectInput
Call prcStartCalculation

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "RORAC was successfully calculated"

End Sub


--- Macro File: Frm_Dea_Ret.frm ---
Attribute VB_Name = "Frm_Dea_Ret"
Attribute VB_Base = "0{F0C22475-90EF-4A4B-8FB6-125B2E5C88E1}{E97A2445-7C88-4B70-BDB0-BA176F71B87B}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
'sub to hide or unhide input fields for detailed input
Private Sub CheckBox1_Click()

If (CheckBox1.value = False) Then
    For i = 1 To 8
        Frm_Dea_Ret("textbox" & Trim(Str(i))).value = ""
        Frm_Dea_Ret("textbox" & Trim(Str(i))).Locked = True
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(175, 178, 180)
    Next
    
    For i = 9 To 9
        Frm_Dea_Ret("textbox" & Trim(Str(i))).Locked = False
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
    Next
    
    CommandButton3.Visible = False
    
Else
    For i = 1 To 2
        Frm_Dea_Ret("textbox" & Trim(Str(i))).value = 0
        Frm_Dea_Ret("textbox" & Trim(Str(i))).Locked = False
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
    Next
    
    For i = 7 To 8
        Frm_Dea_Ret("textbox" & Trim(Str(i))).value = 0
        Frm_Dea_Ret("textbox" & Trim(Str(i))).Locked = False
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
    Next
        
    For i = 9 To 9
        Frm_Dea_Ret("textbox" & Trim(Str(i))).Locked = True
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(175, 178, 180)
    Next
    CommandButton3.Visible = True
End If

End Sub

'sub to copy entered values to portfolio view
Private Sub CommandButton1_Click()

Worksheets("Portfolio").Unprotect Password:="Blattschutz"
Application.ScreenUpdating = False

Dim wksPortfolio As Worksheet
Dim wksIndex As Worksheet
Dim wksinput As Worksheet
Dim clm As Integer

Set wksPortfolio = Sheets("Portfolio")
Set wksinput = Sheets("New Input Mask")
Set wksIndex = Sheets("Index")

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'Check if ebit margin was calculated as a precondition before values can be copied to portfolio
If TextBox9.value = "" And CheckBox1.value = True Then
    MsgBox ("Please calculate Ebit margin first")
    Exit Sub
End If

'depending on setting for decimal separator variables will be declared
If Application.UseSystemSeparators = True Then
    varFINAm = TextBox12.value
    varEBITM = TextBox9.value
    If CheckBox1.value = True Then
        varDEALR = TextBox1.value
        varCoD = TextBox2.value
        varGIM = TextBox3.value
        varCAD = TextBox4.value
        varNIM = TextBox5.value
        varCOR = TextBox6.value
        varOPEX = TextBox7.value
        varISF = TextBox8.value
    End If
Else
    varFINAm = Replace(TextBox12.value, strApp_Dec_Separator, strOS_Dec_Separator)
    varEBITM = Replace(TextBox9.value, strApp_Dec_Separator, strOS_Dec_Separator)
    If CheckBox1.value = True Then
        varDEALR = Replace(TextBox1.value, strApp_Dec_Separator, strOS_Dec_Separator)
        varCoD = Replace(TextBox2.value, strApp_Dec_Separator, strOS_Dec_Separator)
        varGIM = Replace(TextBox3.value, strApp_Dec_Separator, strOS_Dec_Separator)
        varCAD = Replace(TextBox4.value, strApp_Dec_Separator, strOS_Dec_Separator)
        varNIM = Replace(TextBox5.value, strApp_Dec_Separator, strOS_Dec_Separator)
        varCOR = Replace(TextBox6.value, strApp_Dec_Separator, strOS_Dec_Separator)
        varOPEX = Replace(TextBox7.value, strApp_Dec_Separator, strOS_Dec_Separator)
        varISF = Replace(TextBox8.value, strApp_Dec_Separator, strOS_Dec_Separator)
    End If
End If
    
'Check if entered values are valid
If Not IsNumeric(TextBox12.value) Or varFINAm < 0 Then
        TextBox12.BackColor = RGB(255, 0, 0)
        MsgBox "Please enter a correct Financed Volume"
        TextBox12.BackColor = RGB(255, 255, 255)
        Worksheets("Portfolio").Protect Password:="Blattschutz"
        Application.ScreenUpdating = True
        Exit Sub
End If

If Not IsNumeric(TextBox9.value) Then
        TextBox9.BackColor = RGB(255, 0, 0)
        MsgBox "Please enter a correct Ebit Margin"
        TextBox9.BackColor = RGB(255, 255, 255)
        Worksheets("Portfolio").Protect Password:="Blattschutz"
        Application.ScreenUpdating = True
        Exit Sub
End If

'Check if there is free space on portfolio view to enter dealer retail business
clm = 10
Do While wksPortfolio.Cells(11, clm).value <> ""
    clm = clm + 3
    If clm > 52 Then
        MsgBox ("Too many Deals in Portfolio, please delete one or consolidate with another deal")
        Exit Sub
    End If
Loop

'copying of values from form to portfolio sheet either detaild or standard view
If CheckBox1.value = True Then
    wksPortfolio.Cells(11, clm).value = "Dealer Retail Business (Detailed)"
    wksPortfolio.Cells(24, clm).value = CDbl(varDEALR)
    wksPortfolio.Cells(25, clm).value = CDbl(varCoD)
    wksPortfolio.Cells(26, clm).value = CDbl(varGIM)
    wksPortfolio.Cells(27, clm).value = CDbl(varCAD)
    wksPortfolio.Cells(28, clm).value = CDbl(varNIM)
    wksPortfolio.Cells(29, clm).value = CDbl(varCOR)
    wksPortfolio.Cells(30, clm).value = CDbl(varOPEX)
    wksPortfolio.Cells(31, clm).value = CDbl(varISF)
Else
    wksPortfolio.Cells(11, clm).value = "Dealer Retail Business"
End If

wksPortfolio.Cells(19, clm).value = CDbl(varFINAm)
wksPortfolio.Cells(19, clm + 1).value = "T" & [Deal_Currency]
wksPortfolio.Cells(32, clm).value = CDbl(varEBITM)
wksPortfolio.Cells(33, clm).value = [EC_Total] * 100
wksPortfolio.Cells(35, clm).value = wksPortfolio.Cells(32, clm).value / [EC_Total]
If Worksheets("Index").Range("EffectiveMaturityBlended").value > 0 Then
    wksPortfolio.Cells(117, clm).value = 1000 * Worksheets("Index").Range("EffectiveMaturityBlended").value * CDbl(varFINAm)
Else
    wksPortfolio.Cells(117, clm).value = 2400 * CDbl(varFINAm)
End If
wksPortfolio.Cells(54, clm).value = Left([Deal_Currency], 3)
wksPortfolio.Cells(126, clm).value = [FX_Rate]

If [Anz_Dea_Rea] > 0 Then
    Worksheets("Portfolio").Columns("H:H").EntireColumn.Hidden = False
Else
    Worksheets("Portfolio").Columns("H:H").EntireColumn.Hidden = True
End If


Worksheets("Portfolio").Protect Password:="Blattschutz"
Application.ScreenUpdating = True
Unload Me

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

'sub to calculate ebit margin within form
Private Sub CommandButton3_Click()
Dim i As Integer

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

'Validation of IRR and CoD entries
For i = 1 To 2
    If Application.UseSystemSeparators = True Then
        varTB = Frm_Dea_Ret("textbox" & Trim(Str(i))).value
    Else
        varTB = Replace(Frm_Dea_Ret("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
    End If
    If Not IsNumeric(Frm_Dea_Ret("textbox" & Trim(Str(i))).value) Or varTB < 0 Or varTB > 100 Then
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
        MsgBox "Please enter a correct " & Frm_Dea_Ret("label" & Trim(Str(i))).Caption
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
Next

'Validation of OPEX and IDC..
For i = 7 To 8
    If Application.UseSystemSeparators = True Then
        varTB = Frm_Dea_Ret("textbox" & Trim(Str(i))).value
    Else
        varTB = Replace(Frm_Dea_Ret("textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
    End If
    If Not IsNumeric(Frm_Dea_Ret("textbox" & Trim(Str(i))).value) Or varTB > 100 Then
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
        MsgBox "Please enter a correct value for the " & Frm_Dea_Ret("label" & Trim(Str(i))).Caption
        Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
Next

'Calculate Net Ebit Margin
If Application.UseSystemSeparators = True Then
    TextBox3.text = Application.Round(CDbl(TextBox1.text) - CDbl(TextBox2.text), 2)
    TextBox4.text = Application.Round(CDbl(TextBox2.text) * (Worksheets("Index").[EC_Total]), 2)
    TextBox5.text = Application.Round(CDbl(TextBox3.text) + CDbl(TextBox4.text), 2)
    TextBox6.text = Application.Round(CDbl(Worksheets("Index").[CoR]), 2)
    TextBox9.text = Application.Round(CDbl(TextBox5.text) - CDbl(TextBox6.text) - CDbl(TextBox7.text) + CDbl(TextBox8.text), 2)
Else
    varTB1 = Replace(TextBox1.value, Application.DecimalSeparator, strOS_Dec_Separator)
    varTB2 = Replace(TextBox2.value, Application.DecimalSeparator, strOS_Dec_Separator)
    varTB7 = Replace(TextBox7.value, Application.DecimalSeparator, strOS_Dec_Separator)
    varTB8 = Replace(TextBox8.value, Application.DecimalSeparator, strOS_Dec_Separator)
            
    TextBox3.text = Replace(Application.Round(CDbl(varTB1) - CDbl(varTB2), 2), strOS_Dec_Separator, Application.DecimalSeparator)
    TextBox4.text = Replace(Application.Round(CDbl(varTB2) * (Worksheets("Index").[EC_Total]), 2), strOS_Dec_Separator, Application.DecimalSeparator)
    TextBox5.text = Replace(Application.Round(CDbl(varTB1) - CDbl(varTB2) + CDbl(varTB2) * (Worksheets("Index").[EC_Total]), 2), strOS_Dec_Separator, Application.DecimalSeparator)
    TextBox6.text = Replace(Application.Round(CDbl(Worksheets("Index").[CoR]), 2), strOS_Dec_Separator, Application.DecimalSeparator)
    varTB5 = Replace(TextBox5.value, Application.DecimalSeparator, strOS_Dec_Separator)
    varTB6 = Replace(TextBox6.value, Application.DecimalSeparator, strOS_Dec_Separator)
    TextBox9.text = Replace(Application.Round(CDbl(varTB5) - CDbl(varTB6) - CDbl(varTB7) + CDbl(varTB8), 2), strOS_Dec_Separator, Application.DecimalSeparator)
End If

End Sub

'sub to hide detailed input fields as standard setting
Private Sub UserForm_activate()
Label13.Caption = "T" & [Deal_Currency]
CheckBox1.value = False


For i = 1 To 8
    Frm_Dea_Ret("textbox" & Trim(Str(i))).value = ""
    Frm_Dea_Ret("textbox" & Trim(Str(i))).Locked = True
    Frm_Dea_Ret("textbox" & Trim(Str(i))).BackColor = RGB(175, 178, 180)
Next

CommandButton3.Visible = False
End Sub



--- Macro File: Tabelle12.cls ---
Attribute VB_Name = "Tabelle12"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Tabelle6.cls ---
Attribute VB_Name = "Tabelle6"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "ComboBox1, 1, 1, MSForms, ComboBox"
Attribute VB_Control = "ComboBox2, 2, 2, MSForms, ComboBox"
Attribute VB_Control = "ComboBox3, 3, 3, MSForms, ComboBox"
Attribute VB_Control = "ComboBox4, 4, 4, MSForms, ComboBox"
Attribute VB_Control = "CommandButton1, 5, 5, MSForms, CommandButton"
Attribute VB_Control = "CommandButton2, 6, 6, MSForms, CommandButton"
Attribute VB_Control = "ComboBox5, 8, 7, MSForms, ComboBox"
Attribute VB_Control = "ComboBox6, 9, 8, MSForms, ComboBox"
Attribute VB_Control = "ComboBox7, 10, 9, MSForms, ComboBox"
Attribute VB_Control = "ComboBox8, 11, 10, MSForms, ComboBox"
Attribute VB_Control = "ComboBox9, 12, 11, MSForms, ComboBox"
Attribute VB_Control = "ComboBox10, 13, 12, MSForms, ComboBox"
Attribute VB_Control = "ComboBox11, 14, 13, MSForms, ComboBox"
Attribute VB_Control = "ComboBox12, 15, 14, MSForms, ComboBox"
'sub to go back to input sheet
Private Sub CommandButton1_Click()
Worksheets("New Input Mask").Activate
Worksheets("I_and_S").Visible = xlVeryHidden
End Sub

'sub to save entered I und S curves
Private Sub CommandButton2_Click()

Dim i As Integer
Dim intColumn_Save As Integer
intColumn_Save = 0

'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

'Validate First Interests and Spreads Curve --> Check if all combox-elements are filled and if only numeric values as interests and spreads are entered
If Worksheets("I_and_S").Cells(2, 3).value <> "" Then
    If ComboBox1.value = "" Then
        ComboBox1.BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Day Convention Money Market Value"
        ComboBox1.BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox2.value = "" Then
         ComboBox2.BackColor = RGB(255, 0, 0)
         MsgBox "Please add a Day Convention Swap Market Value"
         ComboBox2.BackColor = RGB(255, 255, 255)
         Exit Sub
    ElseIf ComboBox3.value = "" Then
        ComboBox3.BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Compounding Frequency"
        ComboBox3.BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox4.value = "" Then
        ComboBox4.BackColor = RGB(255, 0, 0)
        MsgBox "Please add if annualized"
        ComboBox4.BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
        
    For i = 1 To 11
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 3, 3).value) And Worksheets("I_and_S").Cells(i + 3, 3).value <> "") Then
            Worksheets("I_and_S").Cells(i + 3, 3).Interior.ColorIndex = 3
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 3, 3).Interior.ColorIndex = 0
            Exit Sub
        End If
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 3, 4).value) And Worksheets("I_and_S").Cells(i + 3, 4).value <> "") Then
            Worksheets("I_and_S").Cells(i + 3, 4).Interior.Color = RGB(255, 0, 0)
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 3, 4).Interior.Color = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
    
    For i = 1 To 12
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 19, 3).value) And Worksheets("I_and_S").Cells(i + 19, 3).value <> "") Then
            Worksheets("I_and_S").Cells(i + 19, 3).Interior.ColorIndex = 3
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 19, 3).Interior.ColorIndex = 0
            Exit Sub
        End If
    Next
End If

'Validate Second Interests and Spreads Curve --> Check if all combox-elements are filled and if only numeric values as interests and spreads are entered
If Worksheets("I_and_S").Cells(2, 9).value <> "" Then
    If ComboBox5.value = "" Then
        ComboBox5.BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Day Convention Money Market Value"
        ComboBox5.BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox6.value = "" Then
         ComboBox6.BackColor = RGB(255, 0, 0)
         MsgBox "Please add a Day Convention Swap Market Value"
         ComboBox6.BackColor = RGB(255, 255, 255)
         Exit Sub
    ElseIf ComboBox7.value = "" Then
        ComboBox7.BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Compounding Frequency"
        ComboBox7.BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox8.value = "" Then
        ComboBox8.BackColor = RGB(255, 0, 0)
        MsgBox "Please add if annualized"
        ComboBox8.BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
        
    For i = 1 To 11
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 3, 9).value) And Worksheets("I_and_S").Cells(i + 3, 9).value <> "") Then
            Worksheets("I_and_S").Cells(i + 3, 9).Interior.ColorIndex = 3
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 3, 9).Interior.ColorIndex = 0
            Exit Sub
        End If
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 3, 10).value) And Worksheets("I_and_S").Cells(i + 3, 10).value <> "") Then
            Worksheets("I_and_S").Cells(i + 3, 10).Interior.Color = RGB(255, 0, 0)
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 3, 10).Interior.Color = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
    
    For i = 1 To 12
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 19, 9).value) And Worksheets("I_and_S").Cells(i + 19, 9).value <> "") Then
            Worksheets("I_and_S").Cells(i + 19, 9).Interior.ColorIndex = 3
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 19, 9).Interior.ColorIndex = 0
            Exit Sub
        End If
    Next
End If


'Validate third Interests and Spreads Curve --> Check if all combox-elements are filled and if only numeric values as interests and spreads are entered
If Worksheets("I_and_S").Cells(2, 15).value <> "" Then
    If ComboBox9.value = "" Then
        ComboBox9.BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Day Convention Money Market Value"
        ComboBox9.BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox10.value = "" Then
         ComboBox10.BackColor = RGB(255, 0, 0)
         MsgBox "Please add a Day Convention Swap Market Value"
         ComboBox10.BackColor = RGB(255, 255, 255)
         Exit Sub
    ElseIf ComboBox11.value = "" Then
        ComboBox11.BackColor = RGB(255, 0, 0)
        MsgBox "Please add a Compounding Frequency"
        ComboBox11.BackColor = RGB(255, 255, 255)
        Exit Sub
    ElseIf ComboBox12.value = "" Then
        ComboBox12.BackColor = RGB(255, 0, 0)
        MsgBox "Please add if annualized"
        ComboBox12.BackColor = RGB(255, 255, 255)
        Exit Sub
    End If
        
    For i = 1 To 11
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 3, 15).value) And Worksheets("I_and_S").Cells(i + 3, 15).value <> "") Then
            Worksheets("I_and_S").Cells(i + 3, 15).Interior.ColorIndex = 3
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 3, 15).Interior.ColorIndex = 0
            Exit Sub
        End If
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 3, 16).value) And Worksheets("I_and_S").Cells(i + 3, 16).value <> "") Then
            Worksheets("I_and_S").Cells(i + 3, 16).Interior.Color = RGB(255, 0, 0)
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 3, 16).Interior.Color = RGB(255, 255, 255)
            Exit Sub
        End If
    Next
    
    For i = 1 To 12
        
        If (Not IsNumeric(Worksheets("I_and_S").Cells(i + 19, 16).value) And Worksheets("I_and_S").Cells(i + 19, 16).value <> "") Then
            Worksheets("I_and_S").Cells(i + 19, 16).Interior.ColorIndex = 3
            MsgBox "Please enter a correct value"
            Worksheets("I_and_S").Cells(i + 19, 16).Interior.ColorIndex = 0
            Exit Sub
        End If
    Next
End If

'set to manual calculation to due performance issues
Application.Calculation = xlManual

'Delete old Interest and Spreads on data entities sheet
For i = 1 To 40
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntSpr_DE).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntSpr_DE + 1).value = ""
    Worksheets("Data_Entities").Cells(intEntity_Row + i, posIntSpr_DE + 2).value = ""
Next

'Save First Interests and Spreads Curve
If Worksheets("I_and_S").Cells(2, 3).value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(2, 3).value
    Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + intColumn_Save).value = ComboBox2.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + intColumn_Save).value = ComboBox1.value
        
    'saving values --> text entries will be transfered into a numeric key
    Select Case ComboBox3.value
        Case "Monthly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "1"
        Case "Quarterly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "3"
        Case "Annual"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "12"
        Case Else
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "6"
    End Select
            
    If ComboBox4.value = "No" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "0"
    Else
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "-1"
    End If
    
    Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(19, 3).value
    
    For i = 1 To 11
        Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 3, 3).value
        Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i + 11, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 3, 4).value
    Next
    
    For i = 0 To 10
        Worksheets("Data_Entities").Cells(intEntity_Row + 30 + i, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 21, 3).value
    Next
    Worksheets("Data_Entities").Cells(intEntity_Row + 25, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(20, 3).value
    intColumn_Save = intColumn_Save + 1
End If

'Save Second Interests and Spreads Curve
If Worksheets("I_and_S").Cells(2, 9).value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(2, 9).value
    Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + intColumn_Save).value = ComboBox6.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + intColumn_Save).value = ComboBox5.value
        
    'saving values --> text entries will be transfered into a numeric key
    Select Case ComboBox7.value
        Case "Monthly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "1"
        Case "Quarterly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "3"
        Case "Annual"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "12"
        Case Else
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "6"
    End Select
            
    If ComboBox8.value = "No" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "0"
    Else
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "-1"
    End If
    
    Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(19, 9).value
    
    For i = 1 To 11
        Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 3, 9).value
        Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i + 11, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 3, 10).value
    Next
    
    For i = 0 To 10
        Worksheets("Data_Entities").Cells(intEntity_Row + 30 + i, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 21, 9).value
    Next
        Worksheets("Data_Entities").Cells(intEntity_Row + 25, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(20, 9).value

    intColumn_Save = intColumn_Save + 1
End If

'Save Third Interests and Spreads Curve
If Worksheets("I_and_S").Cells(2, 15).value <> "" Then
    Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(2, 15).value
    Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + intColumn_Save).value = ComboBox10.value
    Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + intColumn_Save).value = ComboBox9.value
        
    'saving values --> text entries will be transfered into a numeric key
    Select Case ComboBox11.value
        Case "Monthly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "1"
        Case "Quarterly"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "3"
        Case "Annual"
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "12"
        Case Else
            Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + intColumn_Save).value = "6"
    End Select
            
    If ComboBox12.value = "No" Then
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "0"
    Else
        Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + intColumn_Save).value = "-1"
    End If
    
    Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(19, 15).value
    
    For i = 1 To 11
        Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 3, 15).value
        Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i + 11, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 3, 16).value
    Next
    
    For i = 0 To 10
        Worksheets("Data_Entities").Cells(intEntity_Row + 30 + i, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(i + 21, 15).value
    Next
    Worksheets("Data_Entities").Cells(intEntity_Row + 25, posIntSpr_DE + intColumn_Save).value = Worksheets("I_and_S").Cells(20, 15).value
    intColumn_Save = intColumn_Save + 1
End If

Worksheets("New Input Mask").Activate
Worksheets("I_and_S").Visible = xlVeryHidden
'Sub to load new I und S from data entities to index sheet
Call dataload(2)

End Sub

Private Sub CommandButton3_Click()
Frm_IS1.Show
End Sub

Private Sub CommandButton4_Click()
Frm_IS2.Show

End Sub

Private Sub CommandButton5_Click()
Frm_IS3.Show

End Sub

'on sheet activation current I und S information will be loaded into template
Private Sub Worksheet_Activate()

Dim i As Integer
Worksheets("I_and_S").Unprotect Password:="Blattschutz"
Application.ScreenUpdating = False
Application.Calculation = xlManual

'Determine current entity position in "Data Entities"-Sheet
intEntity_Row = fct_entity_data_position()

'Load First Interests and Spreads Curve
Worksheets("I_and_S").Cells(2, 3).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE).value
ComboBox1.value = Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE).value
ComboBox2.value = Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE).value
Select Case Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE).value
    Case ""
        ComboBox3.value = ""
    Case "1"
        ComboBox3.value = "Monthly"
    Case "3"
        ComboBox3.value = "Quarterly"
    Case "12"
        ComboBox3.value = "Annual"
    Case Else
        ComboBox3.value = "Semi-annual"
End Select
        
If Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE).value = "0" Then
    ComboBox4.value = "No"
ElseIf Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE).value = "" Then
    ComboBox4.value = ""
Else
    ComboBox4.value = "Yes"
End If
Worksheets("I_and_S").Range("C19").value = Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE).value
For i = 1 To 11
    Worksheets("I_and_S").Cells(i + 3, 3).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE).value
    Worksheets("I_and_S").Cells(i + 3, 4).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i + 11, posIntSpr_DE).value
Next

For i = 21 To 31
    Worksheets("I_and_S").Cells(i, 3).value = Worksheets("Data_Entities").Cells(intEntity_Row + 9 + i, posIntSpr_DE).value
Next
Worksheets("I_and_S").Cells(20, 3).value = Worksheets("Data_Entities").Cells(intEntity_Row + 25, posIntSpr_DE).value

'Load Second Interests and Spreads Curve
Worksheets("I_and_S").Cells(2, 9).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + 1).value
ComboBox5.value = Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + 1).value
ComboBox6.value = Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + 1).value
Select Case Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + 1).value
    Case ""
        ComboBox7.value = ""
    Case "1"
        ComboBox7.value = "Monthly"
    Case "3"
        ComboBox7.value = "Quarterly"
    Case "12"
        ComboBox7.value = "Annual"
    Case Else
        ComboBox7.value = "Semi-annual"
End Select
        
If Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + 1).value = "0" Then
    ComboBox8.value = "No"
ElseIf Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + 1).value = "" Then
    ComboBox8.value = ""
Else
    ComboBox8.value = "Yes"
End If

Worksheets("I_and_S").Range("I19").value = Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + 1).value
For i = 1 To 11
    Worksheets("I_and_S").Cells(i + 3, 9).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE + 1).value
    Worksheets("I_and_S").Cells(i + 3, 10).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i + 11, posIntSpr_DE + 1).value
Next

For i = 21 To 31
    Worksheets("I_and_S").Cells(i, 9).value = Worksheets("Data_Entities").Cells(intEntity_Row + 9 + i, posIntSpr_DE + 1).value
Next
Worksheets("I_and_S").Cells(20, 9).value = Worksheets("Data_Entities").Cells(intEntity_Row + 25, posIntSpr_DE + 1).value

'Load Third Interests and Spreads Curve
Worksheets("I_and_S").Cells(2, 15).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2, posIntSpr_DE + 2).value
ComboBox9.value = Worksheets("Data_Entities").Cells(intEntity_Row + 26, posIntSpr_DE + 2).value
ComboBox10.value = Worksheets("Data_Entities").Cells(intEntity_Row + 27, posIntSpr_DE + 2).value
Select Case Worksheets("Data_Entities").Cells(intEntity_Row + 28, posIntSpr_DE + 2).value
    Case ""
        ComboBox11.value = ""
    Case "1"
        ComboBox11.value = "Monthly"
    Case "3"
        ComboBox11.value = "Quarterly"
    Case "12"
        ComboBox11.value = "Annual"
    Case Else
        ComboBox11.value = "Semi-annual"
End Select
        
If Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + 2).value = "0" Then
    ComboBox12.value = "No"
ElseIf Worksheets("Data_Entities").Cells(intEntity_Row + 29, posIntSpr_DE + 2).value = "" Then
    ComboBox12.value = ""
Else
    ComboBox12.value = "Yes"
End If


Worksheets("I_and_S").Range("o19").value = Worksheets("Data_Entities").Cells(intEntity_Row + 1, posIntSpr_DE + 2).value
For i = 1 To 11
    Worksheets("I_and_S").Cells(i + 3, 15).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i, posIntSpr_DE + 2).value
    Worksheets("I_and_S").Cells(i + 3, 16).value = Worksheets("Data_Entities").Cells(intEntity_Row + 2 + i + 11, posIntSpr_DE + 2).value
Next

For i = 21 To 31
    Worksheets("I_and_S").Cells(i, 15).value = Worksheets("Data_Entities").Cells(intEntity_Row + 9 + i, posIntSpr_DE + 2).value
Next
Worksheets("I_and_S").Cells(20, 15).value = Worksheets("Data_Entities").Cells(intEntity_Row + 25, posIntSpr_DE + 2).value
Application.ScreenUpdating = True
Worksheets("I_and_S").Protect Password:="Blattschutz"
End Sub



--- Macro File: Frm_Cen_Rep.frm ---
Attribute VB_Name = "Frm_Cen_Rep"
Attribute VB_Base = "0{559DBA1E-A942-420B-A146-28815A4DFA21}{0C27ECC2-0914-4F9E-9F3D-95B4980D12F3}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False

'sub to show details for selected deal
Private Sub CommandButton2_Click()
On Error GoTo Fehler
Dim i As Integer
Dim j As Integer

Dim ADOC As New ADODB.Connection
Dim DBS As New ADODB.Recordset
Dim cmd As ADODB.Command
Dim glob_sConnect As String

glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
With ADOC
    .Provider = "Microsoft.Jet.OLEDB.4.0"
    .Properties("Jet OLEDB:Database Password") = pw_DB
    .Mode = adModeReadWrite
    .Open glob_sConnect
End With

Set cmd = New ADODB.Command
'Select to get data from database
       
Dim text As String
Dim ID As Integer
        
'search for selected deals in deal list
For i = 0 To ListBox1.ListCount - 1
    'when find selected deal then gather details and show within textbox
    If ListBox1.Selected(i) Then
        'deal ID of selected deal
        ID = ListBox1.List(i, 0)
        'sql to get data from database incl. where clause for deal_id
        
        cmd.CommandText = "SELECT [Deal_ID], [Quote], [Customer_Name], [Rating_PD_LGD_AddCol], " & _
        "[Vehicle_Type],[Financial_Product_Type],[Balloon_RV],[Credit_Term_in_Months],[Number_of_Vehicles], " & _
        "[Sales_Price_incl_Add_Finan_Items], [Downpayment], [Installment], [Total_Amount_to_Finance], [Customer_Rate],[IDC_Subsidies_and_Fees_periodic]," & _
        "[Deal_Rate],[Cost_of_Debt],[Gross_Interest_Margin],[Capital_Advantage],[Net_Interest_Margin],[Standard_Cost_Credit_Risk], " & _
        "[OPEX], [IDC_Subsidies_and_Fees_periodic],[Net_EBIT_Margin],[Economic_Capital],[RORAC], [Deal_Currency],[Date_of_Storage]" & _
        "FROM [Deal_Storage] Where [Deal_ID] = " & ID
        
        'open database and recordset
        cmd.ActiveConnection = ADOC
        Set DBS = cmd.Execute
        text = ""
        DBS.MoveFirst
        'create string with details-> after each item a line breack will be inserted
        text = text & DBS!Deal_ID & vbCrLf & DBS!Quote & vbCrLf & DBS!Customer_Name & vbCrLf & DBS!Rating_PD_LGD_AddCol & vbCrLf & DBS!Vehicle_Type & vbCrLf & DBS!Financial_Product_Type & vbCrLf
        text = text & Application.Round(DBS!Balloon_RV, 0) & " T" & DBS!Deal_Currency & vbCrLf
        text = text & DBS!Credit_Term_in_Months & vbCrLf & DBS!Number_of_Vehicles & vbCrLf
        text = text & Application.Round(DBS!Sales_Price_incl_Add_Finan_Items, 0) & " T" & DBS!Deal_Currency & vbCrLf
        text = text & Application.Round(DBS!Downpayment, 0) & " T" & DBS!Deal_Currency & vbCrLf
        text = text & Application.Round(DBS!Installment, 0) & DBS!Deal_Currency & vbCrLf
        text = text & Application.Round(DBS!Total_Amount_to_Finance, 0) & " T" & DBS!Deal_Currency & vbCrLf
        text = text & Application.Round(DBS!Customer_Rate, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!IDC_Subsidies_and_Fees_periodic, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Deal_Rate, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Cost_of_Debt, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Gross_Interest_Margin, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Capital_Advantage, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Net_Interest_Margin, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Standard_Cost_Credit_Risk, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!OPEX, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!IDC_Subsidies_and_Fees_periodic, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Net_EBIT_Margin, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!Economic_Capital, 2) & " %" & vbCrLf
        text = text & Application.Round(DBS!RoRAC, 2) & " %" & vbCrLf
        text = text & Format(DBS!Date_of_Storage, "Short Date")
        TextBox1.text = text
        DBS.Close
        ADOC.Close
        Exit For
    End If
Next i

Exit Sub

Fehler:
If Err.Number = 3024 Then
    MsgBox "Database was not found. Please check repository connection.", 0, "An error occured."
Else
    MsgBox "An error occured. Deals could not be imported"
End If

'Close db and recordset in case of an error if they are still open
If DBS Is Nothing Then
Else
    DBS.Close
End If

If ADOC Is Nothing Then
Else
    ADOC.Close
End If

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
End Sub

'sub to import deal from repository to portfolio sheet
Private Sub CommandButton3_Click()

On Error GoTo Fehler

Dim i As Integer
Dim j As Integer
Dim clm As Integer
Dim ID As Integer

 clm = 10
If Worksheets("Portfolio").Columns("AN:BA").Hidden = True Then
   
    
    'check if space is available, no extra deals enabled
    Do While Worksheets("Portfolio").Cells(11, clm).value <> ""
        clm = clm + 3
        If clm > 37 Then
            MsgBox "Too many Deals in Portfolio, please enable additional" & _
            " deal fields by pressing the ""10+ Deals"" button on the portfolio view!"
            Exit Sub
        End If
    Loop
End If

If Worksheets("Portfolio").Columns("AN:BA").Hidden = False Then
    'check if space is available, extra deals enabled
    
    Do While Worksheets("Portfolio").Cells(11, clm).value <> ""
        
        clm = clm + 3
        If clm > 52 Then
            MsgBox "You have reached the maximum number of Deals!"
            Exit Sub
        End If
    Loop
End If
With Application
        .Calculate
        .Calculation = xlManual
        .MaxChange = 0.001
        .ScreenUpdating = False
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Unprotect Password:="Blattschutz"


'Check whether a deal is selected
For i = 0 To ListBox1.ListCount
    If ListBox1.Selected(i) Then
        Exit For
    End If
    If i = ListBox1.ListCount - 1 Then
        MsgBox "No Deal is selected!"
        Exit Sub
    End If
Next i
'search for selected deal in deal list
For i = 0 To ListBox1.ListCount - 1
    If ListBox1.Selected(i) Then
        ID = ListBox1.List(i, 0)
               
        Dim ADOC As New ADODB.Connection
        Dim DBS As New ADODB.Recordset
        Dim cmd As ADODB.Command
        Dim glob_sConnect As String
            
        glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
        With ADOC
            .Provider = "Microsoft.Jet.OLEDB.4.0"
            .Properties("Jet OLEDB:Database Password") = pw_DB
            .Mode = adModeReadWrite
            .Open glob_sConnect
        End With
        
        Dim text As String
        
        Set cmd = New ADODB.Command

        'Select to get data from database
        cmd.CommandText = "SELECT * From [Deal_Storage] Where [Deal_ID] = " & ID
        cmd.ActiveConnection = ADOC
        Set DBS = cmd.Execute

        DBS.MoveFirst
        'Insert data into portfolio sheet
        For j = 2 To 119
            Worksheets("Portfolio").Cells(7 + j, clm).value = DBS.Fields(j).value
        Next
        Worksheets("Portfolio").Cells(7 - 2, clm).value = DBS.Fields(75).value
        Worksheets("Portfolio").Range("D2").value = DBS.Fields(121).value
        Worksheets("Portfolio").Range("D4").value = DBS.Fields(1).value
        Worksheets("Portfolio").Range("D6").value = DBS.Fields(122).value
        DBS.Close
        'exception fro France where "local" data will be imported in respective section on portfolio sheet
        If [Country_Short] = "FRA" Then
            cmd.CommandText = "SELECT * From [Local] where [Deal_ID] = " & ID
            cmd.ActiveConnection = ADOC
            Set DBS = cmd.Execute
            DBS.MoveFirst
            For j = 1 To 20
                Worksheets("Portfolio").Cells(142 + j, clm).value = DBS.Fields(j).value
            Next
            DBS.Close
        End If
        ADOC.Close
        Worksheets("Portfolio").Cells(13, clm + 1).value = "T" & Worksheets("Portfolio").Cells(54, clm)
        Worksheets("Portfolio").Cells(16, clm + 1).value = "T" & Worksheets("Portfolio").Cells(54, clm)
        Worksheets("Portfolio").Cells(17, clm + 1).value = "T" & Worksheets("Portfolio").Cells(54, clm)
        Worksheets("Portfolio").Cells(18, clm + 1).value = Worksheets("Portfolio").Cells(54, clm)
        Worksheets("Portfolio").Cells(19, clm + 1).value = "T" & Worksheets("Portfolio").Cells(54, clm)
        Worksheets("Portfolio").Cells(36, clm + 1).value = "T" & Worksheets("Portfolio").Cells(54, clm)
    End If
Next i

'If Worksheets("Portfolio").Cells(50, clm).value <> "" Then
'        Worksheets("Portfolio").Cells(10, clm).AddComment
'        Worksheets("Portfolio").Cells(10, clm).Comment.text text:=Worksheets("Portfolio").Cells(50, clm).value & " " & Worksheets("Portfolio").Cells(51, clm).value & " " & Worksheets("Portfolio").Cells(44, clm).value
'        Worksheets("Portfolio").Cells(10, clm).Comment.Shape.TextFrame.AutoSize = True
'End If
     
With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
MsgBox "Deal was successfully loaded"
Worksheets("Portfolio").Protect Password:="Blattschutz"

Exit Sub

'Close db and recordset in case of an error if they are still open
Fehler:
If Err.Number = 3024 Then
    MsgBox "Database was not found. Please check repository connection.", 0, "An error occured."
Else
    Debug.Print Err.Description
    MsgBox "An error occured. Deals could not be imported"
End If

If DBS Is Nothing Then
Else
    DBS.Close
End If

If ADOC Is Nothing Then
Else
    ADOC.Close
End If

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
End Sub

Private Sub CommandButton4_Click()
Unload Me
End Sub

'Function to filter deal list
Private Sub CommandButton5_Click()
On Error GoTo Fehler

Dim ADOC As New ADODB.Connection
Dim DBS As New ADODB.Recordset
Dim cmd As ADODB.Command
Dim glob_sConnect As String
    
    glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
    With ADOC
      .Provider = "Microsoft.Jet.OLEDB.4.0"
      .Properties("Jet OLEDB:Database Password") = pw_DB
      .Mode = adModeReadWrite
      .Open glob_sConnect
    End With

Dim f_id As String
Dim f_quote As String
Dim f_cname As String
Dim f_sdate As String
Dim f_ctype As String

'Creation of String for "where-clause"
f_id = TextBox2.value

If TextBox3.value = "" Then
    f_quote = ""
Else
    f_quote = "and [Quote] like '%" & TextBox3.value & "%'"
End If

If TextBox4.value = "" Then
    f_cname = ""
Else
    f_cname = "and [Customer_Name] like '%" & TextBox4.value & "%'"
End If

If TextBox5.value = "" Then
    f_ctype = ""
Else
    f_ctype = "and [CustomerType] like '%" & TextBox5.value & "%'"
End If

If TextBox6.value = "" Then
    f_sdate = ""
Else
    f_sdate = "and [Date_of_storage] like '%" & TextBox6.value & "%'"
End If


Set cmd = New ADODB.Command

cmd.CommandText = "SELECT [Deal_ID], [Quote], [Customer_Name], [CustomerType], [Date_of_storage]" & _
    "FROM [Deal_Storage] where [Deal_ID] like '%" & f_id & "%'" & f_quote & f_cname & f_ctype & f_sdate & _
    "Order By [Deal_ID]"

cmd.ActiveConnection = ADOC
Set DBS = cmd.Execute
    
z = 0

'fill list box containing filter criteria
ListBox1.Clear
With Me.ListBox1
    While DBS.EOF = False
        .AddItem
        .List(z, 0) = DBS.Fields(0).value
        If IsNull(DBS.Fields(1).value) = True Then
            .List(z, 1) = ""
        Else
            .List(z, 1) = DBS.Fields(1).value
        End If
        If IsNull(DBS.Fields(2).value) = True Then
            .List(z, 2) = ""
        Else
            .List(z, 2) = DBS.Fields(2).value
        End If
        .List(z, 3) = DBS.Fields(3).value
        .List(z, 4) = Format(DBS.Fields(4).value, "Short Date")
        DBS.MoveNext
        z = z + 1
    Wend
End With
DBS.Close
ADOC.Close

Exit Sub

'Close db and recordset in case of an error if they are still open
Fehler:
If Err.Number = 3024 Then
    MsgBox "Database was not found. Please check repository connection.", 0, "An error occured."
Else
    MsgBox "An error occured. Deals could not be imported"
End If

If DBS Is Nothing Then
Else
    DBS.Close
End If

If ADOC Is Nothing Then
Else
    ADOC.Close
End If

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
End Sub

'sub to disable filter criteria and show all deals
Private Sub CommandButton6_Click()

On Error GoTo Fehler

Dim ADOC As New ADODB.Connection
Dim DBS As New ADODB.Recordset
Dim cmd As ADODB.Command
Dim glob_sConnect As String
           
glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
With ADOC
    .Provider = "Microsoft.Jet.OLEDB.4.0"
    .Properties("Jet OLEDB:Database Password") = pw_DB
    .Mode = adModeReadWrite
    .Open glob_sConnect
End With

Set cmd = New ADODB.Command

cmd.CommandText = "SELECT [Deal_ID], [Quote], [Customer_Name], [CustomerType], [Date_of_storage]" & _
    "FROM [Deal_Storage]"

cmd.ActiveConnection = ADOC
Set DBS = cmd.Execute
z = 0
ListBox1.Clear
With Me.ListBox1
    While DBS.EOF = False
        .AddItem
        .List(z, 0) = DBS.Fields(0).value
        If IsNull(DBS.Fields(1).value) = True Then
            .List(z, 1) = ""
        Else
            .List(z, 1) = DBS.Fields(1).value
        End If
        If IsNull(DBS.Fields(2).value) = True Then
            .List(z, 2) = ""
        Else
            .List(z, 2) = DBS.Fields(2).value
        End If
        .List(z, 3) = DBS.Fields(3).value
        .List(z, 4) = Format(DBS.Fields(4).value, "Short Date")
        DBS.MoveNext
        z = z + 1
    Wend
End With
DBS.Close
ADOC.Close

Exit Sub

'Close db and recordset in case of an error if they are still open
Fehler:
If Err.Number = 3024 Then
    MsgBox "Database was not found. Please check repository connection.", 0, "An error occured."
Else
    MsgBox "An error occured. Deals could not be imported"
End If

If DBS Is Nothing Then
Else
    DBS.Close
End If

If ADOC Is Nothing Then
Else
    ADOC.Close
End If

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
End Sub





'sub that loads all deals into list box when activating form
Private Sub UserForm_activate()
On Error GoTo Fehler

'Dim db As DAO.Database
'Dim rs As DAO.Recordset

Dim ADOC As New ADODB.Connection
Dim DBS As New ADODB.Recordset
Dim cmd As ADODB.Command
Dim glob_sConnect As String
    
    glob_sConnect = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Worksheets("Index").Range("Path_Repository") & ";"
    With ADOC
      .Provider = "Microsoft.Jet.OLEDB.4.0"
      .Properties("Jet OLEDB:Database Password") = pw_DB
      .Mode = adModeReadWrite
      .Open glob_sConnect
    End With


Label1.Caption = " Deal ID" & vbCrLf & " Quote" & vbCrLf & " Customer Name" & vbCrLf & " Rating / PLF /  PLS / Add.Col" & vbCrLf & " Vehicle Type" & vbCrLf & _
        " Financial Product Type" & vbCrLf & " Balloon/RV" & vbCrLf & " Credit Term in Months" & vbCrLf & " Number of Vehicles" & vbCrLf & " Sales Price(incl.Add.Finan.Items)" & vbCrLf & _
        " Downpayment" & vbCrLf & " Installment" & vbCrLf & " Total Amount to Finance" & vbCrLf & " Customer Rate" & vbCrLf & " IDC , Subsidies And Fees(periodic)" & vbCrLf & " Deal Rate(IRR)" & vbCrLf & _
        " Cost of Debt (100% Matched Funded)" & vbCrLf & " Gross Interest Margin" & vbCrLf & " Capital Advantage" & vbCrLf & " Net Interest Margin" & vbCrLf & " Standard Cost Credit Risk" & vbCrLf & " OPEX" & vbCrLf & _
        " IDC , Subsidies And Fees(periodic)" & vbCrLf & " Net EBIT Margin" & vbCrLf & " Economic Capital" & vbCrLf & " RORAC" & vbCrLf & " Date of Storage"
 
Set cmd = New ADODB.Command

cmd.CommandText = "SELECT [Deal_ID], [Quote], [Customer_Name], [CustomerType], [Date_of_storage]" & _
    "FROM [Deal_Storage] "

cmd.ActiveConnection = ADOC
Set DBS = cmd.Execute

'Set db = OpenDatabase(Worksheets("Index").Range("Path_Repository"), False, False, ";pwd=" + pw_DB)
'Set rs = db.OpenRecordset(s2)
    
With Me.ListBox1
    If DBS.EOF = True Then
        MsgBox "Database is empty!"
        Unload Me
    End If
    While DBS.EOF = False
        .AddItem
        .List(z, 0) = DBS.Fields(0).value
        If IsNull(DBS.Fields(1).value) = True Then
            .List(z, 1) = ""
        Else
            .List(z, 1) = DBS.Fields(1).value
        End If
        If IsNull(DBS.Fields(2).value) = True Then
            .List(z, 2) = ""
        Else
            .List(z, 2) = DBS.Fields(2).value
        End If
        .List(z, 3) = DBS.Fields(3).value
        .List(z, 4) = Format(DBS.Fields(4).value, "Short Date")
        z = z + 1
        DBS.MoveNext
    Wend
End With
DBS.Close
ADOC.Close

Exit Sub

'Close db and recordset in case of an error if they are still open
Fehler:
If Err.Number = 3024 Then
    MsgBox "Database was not found. Please check repository connection.", 0, "An error occured."
Else
    MsgBox "An error occured. Deals could not be stored"
End If

If DBS Is Nothing Then
Else
    DBS.Close
End If

If ADOC Is Nothing Then
Else
    ADOC.Close
End If

With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
End With
ActiveWorkbook.PrecisionAsDisplayed = False
Worksheets("Portfolio").Protect Password:="Blattschutz"
Unload Me

End Sub


--- Macro File: Frm_Mul_Col.frm ---
Attribute VB_Name = "Frm_Mul_Col"
Attribute VB_Base = "0{83B11C7A-1984-472C-852C-43F9D343DAF8}{6F1288F9-8436-4B47-985E-C572ED75DBD1}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False


Dim int_Anzstreams As Integer

Private Sub CommandButton1_Click()
strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

With Application
        .Calculate
        .Calculation = xlManual
        .MaxChange = 0.001
        .ScreenUpdating = False
End With

If TextBox2.value = "" And TextBox3.value <> "" Then
    Frm_Mul_Col("Textbox" & Trim(Str(2))).BackColor = RGB(255, 0, 0)
    MsgBox "Additional Collaterals have to be entered ongoing."
    Frm_Mul_Col("Textbox" & Trim(Str(2))).BackColor = RGB(255, 255, 255)
    With Application
        .Calculation = xlAutomatic
        .MaxChange = 0.001
        .ScreenUpdating = True
    End With
    Exit Sub
End If

For i = 1 To 3
    If Frm_Mul_Col("textbox" & Trim(Str(i))).Visible = True And Frm_Mul_Col("textbox" & Trim(Str(i))).value <> "" Then
        If Application.UseSystemSeparators = True Then
            amount = Frm_Mul_Col("Textbox" & Trim(Str(i))).value
        Else
            amount = Replace(Frm_Mul_Col("Textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator)
        End If

        If (Not IsNumeric(amount)) Then
                Frm_Mul_Col("Textbox" & Trim(Str(i))).BackColor = RGB(255, 0, 0)
                MsgBox "Please enter a correct figure"
                Frm_Mul_Col("Textbox" & Trim(Str(i))).BackColor = RGB(255, 255, 255)
                With Application
                    .Calculation = xlAutomatic
                    .MaxChange = 0.001
                    .ScreenUpdating = True
                End With
                Exit Sub
        End If
    End If
Next

If TextBox1.value <> "" Then
    If Application.UseSystemSeparators = True Then
        Worksheets("New Input Mask").Range("L19").value = CDbl(TextBox1.value)
    Else
        Worksheets("New Input Mask").Range("L19").value = CDbl(Replace(TextBox1.value, strApp_Dec_Separator, strOS_Dec_Separator))
    End If
    Worksheets("New Input Mask").Range("q19").value = ComboBox1.value
    Worksheets("New Input Mask").Range("u19").value = ComboBox11.value

    For i = 2 To 3
        If Frm_Mul_Col("Textbox" & Trim(Str(i))).value <> "" Then
            If Application.UseSystemSeparators = True Then
                Worksheets("Index").Cells(321 + i, 3).value = CDbl(Frm_Mul_Col("Textbox" & Trim(Str(i))).value)
            Else
                Worksheets("Index").Cells(321 + i, 3).value = CDbl(Replace(Frm_Mul_Col("Textbox" & Trim(Str(i))).value, strApp_Dec_Separator, strOS_Dec_Separator))
            End If
            Worksheets("Index").Cells(321 + i, 4).value = Frm_Mul_Col("Combobox" & Trim(Str(i))).value
            Worksheets("Index").Cells(321 + i, 5).value = Frm_Mul_Col("Combobox" & Trim(Str(i)) & Trim(Str(i))).value
        Else
            Worksheets("Index").Cells(321 + i, 3).value = ""
        End If
    Next
Else
    Worksheets("Index").Range("c323").value = ""
    Worksheets("Index").Range("c324").value = ""
End If

With Application
    .Calculation = xlAutomatic
    .MaxChange = 0.001
    .ScreenUpdating = True
End With

Unload Me

End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub

Private Sub CommandButton3_Click()

If int_Anzstreams = 3 Then
    Exit Sub
End If

If Frm_Mul_Col("Textbox" & Trim(Str(int_Anzstreams))).value = "" Then
    MsgBox "Please enter previous collateral stream first."
    Exit Sub
End If

Frm_Mul_Col("textbox" & Trim(Str(int_Anzstreams + 1))).Visible = True
Frm_Mul_Col("Combobox" & Trim(Str(int_Anzstreams + 1))).Visible = True
Frm_Mul_Col("Combobox" & Trim(Str(int_Anzstreams + 1)) & Trim(Str(int_Anzstreams + 1))).Visible = True

int_Anzstreams = int_Anzstreams + 1
End Sub

Private Sub CommandButton4_Click()

If int_Anzstreams = 1 Then
    Exit Sub
End If

Frm_Mul_Col("textbox" & Trim(Str(int_Anzstreams))).value = ""
Frm_Mul_Col("textbox" & Trim(Str(int_Anzstreams))).Visible = False
Frm_Mul_Col("Combobox" & Trim(Str(int_Anzstreams))).Visible = False
Frm_Mul_Col("Combobox" & Trim(Str(int_Anzstreams)) & Trim(Str(int_Anzstreams))).Visible = False

int_Anzstreams = int_Anzstreams - 1

End Sub

Private Sub UserForm_activate()

int_Anzstreams = 1

TextBox1.value = Worksheets("New Input Mask").Range("l19").value
ComboBox1.value = Worksheets("New Input Mask").Range("q19").value

ComboBox11.AddItem Worksheets("Index").Range("AV3").value
ComboBox11.AddItem Worksheets("Index").Range("AV4").value
ComboBox11.AddItem Worksheets("Index").Range("AV5").value
ComboBox11.AddItem Worksheets("Index").Range("AV6").value
ComboBox11.AddItem Worksheets("Index").Range("AV7").value

ComboBox11.value = Worksheets("New Input Mask").Range("u19").value

If Worksheets("Index").Range("c323").value = "" Then
    TextBox2.Visible = False
    ComboBox2.Visible = False
    ComboBox22.Visible = False
    ComboBox2.value = Worksheets("Index").Range("AA3").value
    ComboBox22.AddItem Worksheets("Index").Range("AV3").value
    ComboBox22.AddItem Worksheets("Index").Range("AV4").value
    ComboBox22.AddItem Worksheets("Index").Range("AV5").value
    ComboBox22.AddItem Worksheets("Index").Range("AV6").value
    ComboBox22.AddItem Worksheets("Index").Range("AV7").value
    ComboBox22.ListIndex = 0
Else
    int_Anzstreams = int_Anzstreams + 1
    TextBox2.Visible = True
    TextBox2.value = Worksheets("Index").Range("c323").value
    ComboBox2.Visible = True
    ComboBox22.Visible = True
    ComboBox2.value = Worksheets("Index").Range("d323").value
    ComboBox22.AddItem Worksheets("Index").Range("AV3").value
    ComboBox22.AddItem Worksheets("Index").Range("AV4").value
    ComboBox22.AddItem Worksheets("Index").Range("AV5").value
    ComboBox22.AddItem Worksheets("Index").Range("AV6").value
    ComboBox22.AddItem Worksheets("Index").Range("AV6").value
    ComboBox22.AddItem Worksheets("Index").Range("AV7").value
    ComboBox22.value = Worksheets("Index").Range("e323").value
End If

If Worksheets("Index").Range("c324").value = "" Then
    TextBox3.Visible = False
    ComboBox3.Visible = False
    ComboBox33.Visible = False
    ComboBox3.value = Worksheets("Index").Range("AA3").value
    ComboBox33.AddItem Worksheets("Index").Range("AV3").value
    ComboBox33.AddItem Worksheets("Index").Range("AV4").value
    ComboBox33.AddItem Worksheets("Index").Range("AV5").value
    ComboBox33.AddItem Worksheets("Index").Range("AV6").value
    ComboBox33.AddItem Worksheets("Index").Range("AV7").value
    ComboBox33.ListIndex = 0
Else
    int_Anzstreams = int_Anzstreams + 1
    TextBox3.Visible = True
    TextBox3.value = Worksheets("Index").Range("c324").value
    ComboBox3.Visible = True
    ComboBox33.Visible = True
    ComboBox3.value = Worksheets("Index").Range("d324").value
    ComboBox33.AddItem Worksheets("Index").Range("AV3").value
    ComboBox33.AddItem Worksheets("Index").Range("AV4").value
    ComboBox33.AddItem Worksheets("Index").Range("AV5").value
    ComboBox33.AddItem Worksheets("Index").Range("AV6").value
    ComboBox33.AddItem Worksheets("Index").Range("AV6").value
    ComboBox22.AddItem Worksheets("Index").Range("AV7").value
    ComboBox33.value = Worksheets("Index").Range("e324").value
End If

End Sub


--- Macro File: Sheet5.cls ---
Attribute VB_Name = "Sheet5"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Frm_Password_IS.frm ---
Attribute VB_Name = "Frm_Password_IS"
Attribute VB_Base = "0{DB39418D-9A90-4E93-9AFD-C3AF6E307E1E}{A2291DD2-D76E-47D5-9262-F06C7039B87F}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private Sub CommandButton1_Click()
Dim pw As String

pw = TextBox1.text
Dim t As Variant
Dim value As String

If Worksheets("Index").Range("Country_Short") = "RUS" Then
    If pw <> Application.VLookup("RUS", ActiveWorkbook.Worksheets("PW").Range("A1:C40"), 3, 0) Then
        MsgBox ("Wrong Password, please try again.")
        TextBox1.text = ""
    Else
        Unload Me
        Worksheets("I_and_S").Visible = True
        Worksheets("I_and_S").Activate
    End If
Else

    If pw <> "parameter" Then
        MsgBox ("Wrong Password, please try again.")
        TextBox1.text = ""
    Else
        Unload Me
        Worksheets("I_and_S").Visible = True
        Worksheets("I_and_S").Activate
    End If
End If
End Sub

Private Sub CommandButton2_Click()
Unload Me
End Sub


--- Macro File: Tabelle11.cls ---
Attribute VB_Name = "Tabelle11"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "CommandButton7, 24, 18, MSForms, CommandButton"
Private Sub CommandButton2_Click()
If [Country_Short] = "USA" Then
    Range("L12:M21").Cells.ClearContents
    Range("H12:I21").Cells.ClearContents
    Worksheets("New Input Mask").Range("E12").value = [PD_US] * 100
    If [R_S_US] = "Yes" Then
        Worksheets("New Input Mask").Range("E25").value = "Dealer Lease Line"
    End If
End If
Worksheets("Local_Sheet").ScrollArea = ""
Worksheets("New Input Mask").Select
Worksheets("New Input Mask").[e8].Activate
End Sub

Private Sub CommandButton3_Click()
Worksheets("Local_Sheet").ScrollArea = ""
Worksheets("New Input Mask").Select
Worksheets("New Input Mask").[e8].Activate
End Sub

Private Sub CommandButton4_Click()
Worksheets("Local_Sheet").ScrollArea = ""
Worksheets("New Input Mask").Select
Worksheets("New Input Mask").[e8].Activate
End Sub

Private Sub OptionButton1_Click()
Worksheets("Local_Sheet").Range("A305").value = 1
End Sub

Private Sub OptionButton2_Click()
Worksheets("Local_Sheet").Range("A305").value = 2
End Sub

'Exception for France
Private Sub Worksheet_Change(ByVal Target As Range)
If Target.Address = "$B$11" Then
    If Target.value = "Assurance matériel" Then
    Worksheets("Local_Sheet").Range("B13").value = "all"
    End If
End If

If Target.Address = "$B$19" Then
    If Target.value = "Assurance matériel" Then
    Worksheets("Local_Sheet").Range("B21").value = "all"
    End If
End If

'Trigger calculation for South Africa, added 15.10.2014 by Robert Szakacs
'Show PLS and calculation date, added 15.04.2015 by Teng Teng
'for automating/batch purposes
If Target.Address = "$ZZ$1" Then
    Call prcStartCalculation
    Call LGD_button_CalcDate
End If

'Triggering Target Rate calculation for South Africe, added 21.10.2014 by Robert Szakacs
If Target.Address = "$AAA$1" Then
    [Target_RORAC] = Target.value / 100
    [Target_RORAC_Case] = "Yes"
    [Target_Type] = "1"
    Call prcStartCalculation
    Worksheets("Local_Sheet").Range("AAA2").value = [Target_Rate] * 100
    [Target_RORAC_Case] = "No"
End If

'Triggering Required Subsidies calculation for South Africe, added 26.03.2014 by Teng Teng
If Target.Address = "$AAB$1" Then
    [Target_RORAC] = Target.value / 100
    [Target_RORAC_Case] = "Yes"
    [Target_Type] = "3"
    Call prcStartCalculation
    Worksheets("Local_Sheet").Range("AAB2").value = [Target_Rate]
    [Target_RORAC_Case] = "No"
End If

End Sub

Private Sub CommandButton1_Click()

Application.EnableEvents = True

Call unprotectInput

Dim zelle As Range
Dim lItem As Integer
Dim AnzMonths As Integer
Dim intLastInst
Dim bolInp_OK As Boolean

lItem = 0


bolInp_OK = fct_checkInput()

If bolInp_OK = False Then
    Call protectInput
    Exit Sub
End If

'MessageBox If stored Interest and Spreads older than one week or one month (Exception for Mexico, Spain and Thailand)
If Worksheets("Index").Range("Expiry_Date_IS") < Date And [Country_Short] <> "MEX" Then
    If [Country_Short] = "RUS" Then
        MsgBox ("Stored Interests and Spreads are older than one week (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    ElseIf [Country_Short] = "THA" Then
        MsgBox ("Stored Interests and Spreads are older than two weeks (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    Else
        MsgBox ("Stored Interests and Spreads are older than one month (" & Str(Worksheets("Index").Range("Quotation_Date").value) & ")" & vbCrLf & "Please update!")
    End If
End If

'MessageBox If stored Manual Cash Flow is used
If Worksheets("Index").Range("Manual_CF_Flag") = 1 Then
    MsgBox ("Manual Cash Flow is active and will be considered for calculation!")
End If

'MessageBox If more than one collateral is added
If Worksheets("Index").Range("C323").value <> "" Or Worksheets("Index").Range("C324").value <> "" Then
    MsgBox ("More than one additional collateral was added and will be considered for calculation!")
End If

'MessageBox If stored Acceleraded Payment is used
If Worksheets("Index").Range("Accelerated_Payment_Flag") = 1 Then
    MsgBox ("Accelerated Payment is active and will be considered for calculation!")
    Worksheets("Index").Range("write_mcf").value = "yes"
    Worksheets("Index").Range("Accelerated_Payment_Flag") = 0
    Call prcStartCalculation
    Worksheets("Index").Range("write_mcf").value = "no"
    Worksheets("Index").Range("Accelerated_Payment_Flag") = 1
    Application.Calculate
End If


'Start RORAC-Calculation
Call prcStartCalculation

Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "RORAC was successfully calculated"
Call France
End Sub

Private Sub CommandButton7_Click()
If [Country_Short] = "USA" Then
    Range("L12:M21").Cells.ClearContents
    Range("H12:I21").Cells.ClearContents
    Worksheets("New Input Mask").Range("E12").value = [PD_US] * 100
    If [R_S_US] = "Yes" Then
        Worksheets("New Input Mask").Range("E25").value = "Dealer Lease Line"
    End If
End If

If [Country_Short] = "THA" Then
    Worksheets("New Input Mask").Range("E8") = "Dealer"
    Worksheets("New Input Mask").Range("E10") = 6
    Worksheets("New Input Mask").Range("E25") = "Dealer Floorplan Mercerdes Benz Used Vehicle_V1.0_4.Jun 2003_TH"
    Worksheets("New Input Mask").Range("G27") = "Dealer"
    Worksheets("New Input Mask").Range("H41") = Worksheets("Local_Sheet").Range("E5") * 100
    Worksheets("New Input Mask").Range("H54") = Worksheets("Local_Sheet").Range("E4") * 100
    Worksheets("New Input Mask").ComboBox5.value = "% per anno"
    Application.EnableEvents = True
End If

Worksheets("Local_Sheet").ScrollArea = ""
Worksheets("New Input Mask").Select
Worksheets("New Input Mask").[e8].Activate

End Sub






--- Macro File: Tabelle14.cls ---
Attribute VB_Name = "Tabelle14"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Tabelle13.cls ---
Attribute VB_Name = "Tabelle13"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Attribute VB_Control = "CommandButton1, 1, 0, MSForms, CommandButton"
'sub to open form for tool initialization
Private Sub CommandButton1_Click()
Frm_Ini.Show
End Sub


--- Macro File: Sheet2.cls ---
Attribute VB_Name = "Sheet2"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Sheet4.cls ---
Attribute VB_Name = "Sheet4"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True


--- Macro File: Frm_Target_Instal.frm ---
Attribute VB_Name = "Frm_Target_Instal"
Attribute VB_Base = "0{FF10DF12-5E46-4A84-A7BB-6B82CC9E7D46}{008EEB9A-1D7A-4C56-9A16-C6CE38454E7D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Private Sub UserForm_activate()
Label2.Caption = "Required " & Worksheets("New Input Mask").ComboBox3.value
CommandButton1.Caption = "Calculate " & Worksheets("New Input Mask").ComboBox3.value
Label3.Caption = Left([Deal_Currency], 3)
End Sub

Private Sub CommandButton1_Click()

Dim value As Double


If (TextBox1.value <> "" And Not IsNumeric(TextBox1.value)) Or (TextBox1.value = "") Then
        MsgBox "Please enter a correct Target Instalment"
        Exit Sub
End If

strOS_Dec_Separator = fct_SystemSetting("sdecimal")
strApp_Dec_Separator = Application.DecimalSeparator

If Application.UseSystemSeparators = True Then
    value = CDbl(TextBox1.value)
Else
    value = CDbl(Replace(TextBox1.value, strApp_Dec_Separator, strOS_Dec_Separator))
End If

[Target_Instalment] = value

If [Start_RORAC] <= -1 Then
    MsgBox "Please enter a higher Target Instalment"
    Exit Sub
End If

[Target_RORAC_Case] = "Yes"
[Target_Type] = "4"
Call unprotectInput

Call prcStartCalculation

If Application.UseSystemSeparators = True Then
    TextBox2.value = WorksheetFunction.Round([Target_Rate] * 100, 2)
Else
    TextBox2.value = Replace(WorksheetFunction.Round([Target_Rate] * 100, 2), strOS_Dec_Separator, strApp_Dec_Separator)
End If

Label5.Caption = [Target_Rate] * 100
[Target_RORAC_Case] = "No"

Call protectInput

End Sub

Private Sub CommandButton2_Click()

If (TextBox2.value = "") Then
        MsgBox "Please Calculate a Target Customer Rate"
        Exit Sub
End If

'Mexico case
If [Country_Short] = "MEX" And [tax_rate] > 0 And [TaxRefBase] = "Interest" And [TaxQuotationType] = 1 Then
    If [IDC] - [Subsidies] <> 0 Then
    Worksheets("New Input Mask").Range("H54").value = CDbl(Label5.Caption)
    Else
    Worksheets("New Input Mask").Range("H54").value = CDbl(Label5.Caption) / (1 + [tax_rate])
    End If
    [Nom_CR_MCF] = CDbl(Label5.Caption) / (1 + [tax_rate])
Else
    If CDbl(Label5.Caption) < 0 Then
        MsgBox "Please enter a valid Customer/Buy rate between 0 and 100"
        Worksheets("New Input Mask").Range("H54").value = ""
        Application.StatusBar = ""
        Unload Me
        Exit Sub
    Else
        Worksheets("New Input Mask").Range("H54").value = CDbl(Label5.Caption)
        [Nom_CR_MCF] = CDbl(Label5.Caption)
    End If
End If

Unload Me

Call unprotectInput

Call prcStartCalculation

[Target_RORAC_Case] = "No"

Call LGD_button_CalcDate

'Protect Sheet after successfully RORAC Calculation
Call protectInput

'MsgBox "RORAC was successfully calculated"
Application.StatusBar = "Target RORAC was successfully calculated"
End Sub

Private Sub CommandButton3_Click()
[Target_RORAC_Case] = "No"
Unload Me
End Sub



--- Macro File: Tabelle16.cls ---
Attribute VB_Name = "Tabelle16"
Attribute VB_Base = "0{00020820-0000-0000-C000-000000000046}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = True
Public Sub zurueck()
If [Country_Short] = "USA" Then
    Range("L12:M21").Cells.ClearContents
    Range("H12:I21").Cells.ClearContents
    Worksheets("New Input Mask").Range("E12").value = [PD_US] * 100
    If [R_S_US] = "Yes" Then
        Worksheets("New Input Mask").Range("E25").value = "Dealer Lease Line"
    End If
End If

If [Country_Short] = "THA" Then
    Worksheets("New Input Mask").Range("E8") = "Dealer"
    Worksheets("New Input Mask").Range("E10") = 6
    Worksheets("New Input Mask").Range("E25") = "Dealer Floorplan Mercerdes Benz Used Vehicle_V1.0_4.Jun 2003_TH"
    Worksheets("New Input Mask").Range("G27") = "Dealer"
    Worksheets("New Input Mask").Range("H41") = Worksheets("Local_Sheet").Range("E5") * 100
    Worksheets("New Input Mask").Range("H54") = Worksheets("Local_Sheet").Range("E4") * 100
    Worksheets("New Input Mask").ComboBox5.value = "% per anno"
    Application.EnableEvents = True
End If

Worksheets("Local_Sheet").ScrollArea = ""
Worksheets("New Input Mask").Select
Worksheets("New Input Mask").[e8].Activate

End Sub



