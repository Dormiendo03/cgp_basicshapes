Public Class frmTrafficSimulation

    ' ===== ENUMERATIONS =====
    Private Enum LightState
        Red = 0
        Yellow = 1
        Green = 2
    End Enum

    ' ===== CLASS VARIABLES =====
    Private currentPhase As Integer = 0
    Private phaseTimer As Integer = 0
    Private greenTime As Integer = 5
    Private yellowTime As Integer = 2
    Private elapsedTime As Integer = 0
    Private isRunning As Boolean = False

    ' Pedestrian variables
    Private pedestrianRequests As Dictionary(Of String, Boolean)
    Private pedestrianActive As Dictionary(Of String, Boolean)
    Private isInPedestrianMode As Boolean = False
    Private pedestrianModeTimer As Integer = 0
    Private pedestrianPhase As Integer = 0
    Private activePedestrianDirection As String = ""
    Private activePedestrianIntersection As Integer = 0

    ' Pedestrian timing
    Private pedestrianYellowTime As Integer = 3
    Private pedestrianRedTime As Integer = 6
    Private pedestrianCrossingTime As Integer = 10

    ' ===== FORM LOAD =====
    Private Sub frmTrafficSimulation_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitializePedestrianTracking()
        InitializeAllLights()
        AttachEventHandlers()
        UpdateStatus("System Ready - Click START to begin")
    End Sub

    ' ===== INITIALIZE PEDESTRIAN TRACKING =====
    Private Sub InitializePedestrianTracking()
        pedestrianRequests = New Dictionary(Of String, Boolean)
        pedestrianActive = New Dictionary(Of String, Boolean)

        For intersection As Integer = 1 To 4
            For Each direction In {"N", "S", "E", "W"}
                Dim key As String = $"{direction}{intersection}"
                pedestrianRequests(key) = False
                pedestrianActive(key) = False
            Next
        Next
    End Sub

    ' ===== INITIALIZE ALL LIGHTS TO RED =====
    Private Sub InitializeAllLights()
        For intersection As Integer = 1 To 4
            For Each direction In {"N", "S", "E", "W"}
                SetTrafficLight(intersection, direction, LightState.Red)
                SetPedestrianLight(intersection, direction, LightState.Red)
            Next
        Next
    End Sub

    ' ===== SET TRAFFIC LIGHT COLOR =====
    Private Sub SetTrafficLight(intersection As Integer, direction As String, state As LightState)
        Try
            Dim redControl As Control = FindControlByName($"pnl{direction}{intersection}Red")
            Dim yellowControl As Control = FindControlByName($"pnl{direction}{intersection}Yellow")
            Dim greenControl As Control = FindControlByName($"pnl{direction}{intersection}Green")

            If redControl Is Nothing Or yellowControl Is Nothing Or greenControl Is Nothing Then
                Exit Sub
            End If

            ' Reset all colors to gray
            redControl.BackColor = Color.Gray
            yellowControl.BackColor = Color.Gray
            greenControl.BackColor = Color.Gray

            ' Set the appropriate color
            If state = LightState.Red Then
                redControl.BackColor = Color.Red
            ElseIf state = LightState.Yellow Then
                yellowControl.BackColor = Color.Yellow
            ElseIf state = LightState.Green Then
                greenControl.BackColor = Color.Green
            End If
        Catch ex As Exception
            ' Silent error handling
        End Try
    End Sub

    ' ===== SET PEDESTRIAN LIGHT COLOR =====
    Private Sub SetPedestrianLight(intersection As Integer, direction As String, state As LightState)
        Try
            Dim controlName As String = $"pnl{direction}{intersection}PedLight"
            Dim pedControl As Control = FindControlByName(controlName)

            If pedControl Is Nothing Then
                ' Control not found - for debugging
                Exit Sub
            End If

            If state = LightState.Red Then
                pedControl.BackColor = Color.Red
            ElseIf state = LightState.Green Then
                pedControl.BackColor = Color.Green
            End If
        Catch ex As Exception
            ' Silent error handling
        End Try
    End Sub

    ' ===== FIND CONTROL BY NAME =====
    Private Function FindControlByName(name As String) As Control
        Try
            Dim foundControls As Control() = Me.Controls.Find(name, True)
            If foundControls.Length > 0 Then
                Return foundControls(0)
            End If
        Catch
        End Try
        Return Nothing
    End Function

    ' ===== FIND BUTTON BY NAME =====
    Private Function FindButtonByName(name As String) As Button
        Try
            Dim ctrl As Control = FindControlByName(name)
            If TypeOf ctrl Is Button Then
                Return CType(ctrl, Button)
            End If
        Catch
        End Try
        Return Nothing
    End Function

    ' ===== ATTACH EVENT HANDLERS =====
    Private Sub AttachEventHandlers()
        Try
            ' Attach all pedestrian button handlers
            For intersection As Integer = 1 To 4
                For Each direction In {"N", "S", "E", "W"}
                    Dim buttonName As String = $"btn{direction}{intersection}Pedestrian"
                    Dim btn As Button = FindButtonByName(buttonName)
                    If btn IsNot Nothing Then
                        AddHandler btn.Click, AddressOf PedestrianButton_Click
                    End If
                Next
            Next

            ' Control Buttons
            AddHandler btnStart.Click, AddressOf btnStart_Click
            AddHandler btnStop.Click, AddressOf btnStop_Click
            AddHandler btnReset.Click, AddressOf btnReset_Click

            ' Timers
            AddHandler tmrTrafficControl.Tick, AddressOf TrafficControl_Tick
            AddHandler tmrElapsedTime.Tick, AddressOf ElapsedTime_Tick

        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}")
        End Try
    End Sub

    ' ===== START BUTTON =====
    Private Sub btnStart_Click(sender As Object, e As EventArgs)
        isRunning = True
        currentPhase = 1
        phaseTimer = 0
        elapsedTime = 0
        isInPedestrianMode = False

        btnStart.Enabled = False
        btnStop.Enabled = True
        btnReset.Enabled = False

        tmrTrafficControl.Enabled = True
        tmrElapsedTime.Enabled = True

        UpdateStatus("Simulation Running - N/S Lights GREEN")
    End Sub

    ' ===== STOP BUTTON =====
    Private Sub btnStop_Click(sender As Object, e As EventArgs)
        isRunning = False
        tmrTrafficControl.Enabled = False
        tmrElapsedTime.Enabled = False

        btnStart.Enabled = True
        btnStop.Enabled = False
        btnReset.Enabled = True

        UpdateStatus("Simulation Paused")
    End Sub

    ' ===== RESET BUTTON =====
    Private Sub btnReset_Click(sender As Object, e As EventArgs)
        isRunning = False
        tmrTrafficControl.Enabled = False
        tmrElapsedTime.Enabled = False

        currentPhase = 0
        phaseTimer = 0
        elapsedTime = 0
        isInPedestrianMode = False
        pedestrianModeTimer = 0
        pedestrianPhase = 0
        activePedestrianDirection = ""
        activePedestrianIntersection = 0
        lblTimer.Text = "Elapsed: 0s"

        InitializePedestrianTracking()
        InitializeAllLights()

        btnStart.Enabled = True
        btnStop.Enabled = False
        btnReset.Enabled = True

        UpdateStatus("Simulation Reset - Click START to begin")
    End Sub

    ' ===== MAIN TRAFFIC CONTROL TIMER =====
    Private Sub TrafficControl_Tick(sender As Object, e As EventArgs)
        If Not isRunning Then Exit Sub

        ' ===== PEDESTRIAN MODE (Only affects the active intersection) =====
        If isInPedestrianMode Then
            pedestrianModeTimer += 1

            Select Case pedestrianPhase
                ' Phase 0: Yellow phase (3 seconds) - ONLY at active intersection
                Case 0
                    If pedestrianModeTimer <= pedestrianYellowTime Then
                        UpdateStatus($"🚶 INT{activePedestrianIntersection} {activePedestrianDirection}: Perpendicular lights YELLOW ({pedestrianYellowTime - pedestrianModeTimer + 1}s)...")
                    Else
                        ' Switch to RED phase
                        pedestrianPhase = 1
                        pedestrianModeTimer = 0

                        ' Turn perpendicular lights RED at THIS INTERSECTION ONLY
                        Select Case activePedestrianDirection
                            Case "N", "S"
                                SetTrafficLight(activePedestrianIntersection, "E", LightState.Red)
                                SetTrafficLight(activePedestrianIntersection, "W", LightState.Red)
                                SetTrafficLight(activePedestrianIntersection, "N", LightState.Red)
                                SetTrafficLight(activePedestrianIntersection, "S", LightState.Red)

                            Case "E", "W"
                                SetTrafficLight(activePedestrianIntersection, "N", LightState.Red)
                                SetTrafficLight(activePedestrianIntersection, "S", LightState.Red)
                                SetTrafficLight(activePedestrianIntersection, "E", LightState.Red)
                                SetTrafficLight(activePedestrianIntersection, "W", LightState.Red)
                        End Select

                        UpdateStatus($"🚶 INT{activePedestrianIntersection} {activePedestrianDirection}: All lights RED (6s)...")
                    End If

                ' Phase 1: Red phase (6 seconds) - ONLY at active intersection
                Case 1
                    If pedestrianModeTimer <= pedestrianRedTime Then
                        Dim timeRemaining As Integer = pedestrianRedTime - pedestrianModeTimer
                        UpdateStatus($"🚶 INT{activePedestrianIntersection} {activePedestrianDirection}: All RED ({timeRemaining}s)...")
                    Else
                        ' Switch to crossing phase
                        pedestrianPhase = 2
                        pedestrianModeTimer = 0

                        ' Turn pedestrian light GREEN at THIS INTERSECTION ONLY
                        SetPedestrianLight(activePedestrianIntersection, activePedestrianDirection, LightState.Green)
                        UpdateStatus($"🚶 INT{activePedestrianIntersection} {activePedestrianDirection}: CROSSING (10s)...")
                    End If

                ' Phase 2: Pedestrian crossing phase (10 seconds) - ONLY at active intersection
                Case 2
                    If pedestrianModeTimer <= pedestrianCrossingTime Then
                        Dim timeRemaining As Integer = pedestrianCrossingTime - pedestrianModeTimer
                        UpdateStatus($"🚶 INT{activePedestrianIntersection} {activePedestrianDirection}: Crossing {timeRemaining}s remaining...")
                    Else
                        ' Crossing done - restore normal traffic at this intersection only
                        SetPedestrianLight(activePedestrianIntersection, activePedestrianDirection, LightState.Red)

                        ' Reset all pedestrian lights at this intersection to RED first
                        SetPedestrianLight(activePedestrianIntersection, "N", LightState.Red)
                        SetPedestrianLight(activePedestrianIntersection, "S", LightState.Red)
                        SetPedestrianLight(activePedestrianIntersection, "E", LightState.Red)
                        SetPedestrianLight(activePedestrianIntersection, "W", LightState.Red)

                        ' Also reset all traffic lights at this intersection to RED first
                        SetTrafficLight(activePedestrianIntersection, "N", LightState.Red)
                        SetTrafficLight(activePedestrianIntersection, "S", LightState.Red)
                        SetTrafficLight(activePedestrianIntersection, "E", LightState.Red)
                        SetTrafficLight(activePedestrianIntersection, "W", LightState.Red)

                        isInPedestrianMode = False
                        pedestrianModeTimer = 0
                        pedestrianPhase = 0
                        activePedestrianDirection = ""
                        activePedestrianIntersection = 0

                        ' Restore to normal traffic cycle
                        RestoreNormalTrafficCycle()

                        UpdateStatus("Pedestrian crossing complete - resuming normal traffic")
                    End If
            End Select

            ' Continue updating OTHER intersections in normal mode while one is in pedestrian mode
            UpdateNormalTrafficOtherIntersections()

            ' Make sure to restore pedestrian light after other updates
            If pedestrianPhase = 2 Then
                SetPedestrianLight(activePedestrianIntersection, activePedestrianDirection, LightState.Green)
            End If

            Exit Sub
        End If

        ' ===== NORMAL TRAFFIC MODE (All intersections synchronized) =====
        phaseTimer += 1

        Select Case currentPhase
            ' Phase 1: N/S Green, E/W Red
            Case 1
                If phaseTimer <= greenTime Then
                    ' N/S is GREEN
                    For i As Integer = 1 To 4
                        SetTrafficLight(i, "N", LightState.Green)
                        SetTrafficLight(i, "S", LightState.Green)
                        SetTrafficLight(i, "E", LightState.Red)
                        SetTrafficLight(i, "W", LightState.Red)

                        ' ✅ E/W pedestrians RED when N/S traffic is GREEN
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)

                        ' N/S pedestrians GREEN when N/S traffic is GREEN
                        SetPedestrianLight(i, "N", LightState.Green)
                        SetPedestrianLight(i, "S", LightState.Green)
                    Next
                    UpdateStatus($"N/S GREEN | E/W RED | 🚶 N/S Pedestrians Can Cross ({phaseTimer}s / {greenTime}s)")

                ElseIf phaseTimer <= greenTime + yellowTime Then
                    ' N/S is YELLOW, E/W is RED
                    For i As Integer = 1 To 4
                        SetTrafficLight(i, "N", LightState.Yellow)
                        SetTrafficLight(i, "S", LightState.Yellow)
                        SetTrafficLight(i, "E", LightState.Red)
                        SetTrafficLight(i, "W", LightState.Red)

                        ' Keep pedestrian lights RED when vehicle lights are yellow
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    Next
                    UpdateStatus($"N/S YELLOW | E/W RED")

                Else
                    currentPhase = 2
                    phaseTimer = 0
                End If

            ' Phase 2: E/W Green, N/S Red
            Case 2
                If phaseTimer <= greenTime Then
                    ' E/W is GREEN
                    For i As Integer = 1 To 4
                        SetTrafficLight(i, "N", LightState.Red)
                        SetTrafficLight(i, "S", LightState.Red)
                        SetTrafficLight(i, "E", LightState.Green)
                        SetTrafficLight(i, "W", LightState.Green)

                        ' ✅ N/S pedestrians RED when E/W traffic is GREEN
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)

                        ' E/W pedestrians GREEN when E/W traffic is GREEN
                        SetPedestrianLight(i, "E", LightState.Green)
                        SetPedestrianLight(i, "W", LightState.Green)
                    Next
                    UpdateStatus($"E/W GREEN | N/S RED | 🚶 E/W Pedestrians Can Cross ({phaseTimer}s / {greenTime}s)")

                ElseIf phaseTimer <= greenTime + yellowTime Then
                    ' E/W is YELLOW, N/S is RED
                    For i As Integer = 1 To 4
                        SetTrafficLight(i, "N", LightState.Red)
                        SetTrafficLight(i, "S", LightState.Red)
                        SetTrafficLight(i, "E", LightState.Yellow)
                        SetTrafficLight(i, "W", LightState.Yellow)

                        ' Keep pedestrian lights RED when vehicle lights are yellow
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    Next
                    UpdateStatus($"E/W YELLOW | N/S RED")

                Else
                    currentPhase = 1
                    phaseTimer = 0
                End If
        End Select

        ' Check for pedestrian button requests
        CheckPedestrianRequests()
    End Sub

    ' ===== UPDATE NORMAL TRAFFIC FOR OTHER INTERSECTIONS =====
    Private Sub UpdateNormalTrafficOtherIntersections()
        For i As Integer = 1 To 4
            ' Skip the active pedestrian intersection
            If i = activePedestrianIntersection Then
                Continue For
            End If

            ' Apply normal traffic pattern to other intersections
            Select Case currentPhase
                Case 1
                    If phaseTimer <= greenTime Then
                        SetTrafficLight(i, "N", LightState.Green)
                        SetTrafficLight(i, "S", LightState.Green)
                        SetTrafficLight(i, "E", LightState.Red)
                        SetTrafficLight(i, "W", LightState.Red)
                        SetPedestrianLight(i, "N", LightState.Green)
                        SetPedestrianLight(i, "S", LightState.Green)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    ElseIf phaseTimer <= greenTime + yellowTime Then
                        SetTrafficLight(i, "N", LightState.Yellow)
                        SetTrafficLight(i, "S", LightState.Yellow)
                        SetTrafficLight(i, "E", LightState.Red)
                        SetTrafficLight(i, "W", LightState.Red)
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    End If

                Case 2
                    If phaseTimer <= greenTime Then
                        SetTrafficLight(i, "N", LightState.Red)
                        SetTrafficLight(i, "S", LightState.Red)
                        SetTrafficLight(i, "E", LightState.Green)
                        SetTrafficLight(i, "W", LightState.Green)
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Green)
                        SetPedestrianLight(i, "W", LightState.Green)
                    ElseIf phaseTimer <= greenTime + yellowTime Then
                        SetTrafficLight(i, "N", LightState.Red)
                        SetTrafficLight(i, "S", LightState.Red)
                        SetTrafficLight(i, "E", LightState.Yellow)
                        SetTrafficLight(i, "W", LightState.Yellow)
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    End If
            End Select
        Next
    End Sub

    ' ===== RESTORE NORMAL TRAFFIC CYCLE FOR ALL INTERSECTIONS =====
    Private Sub RestoreNormalTrafficCycle()
        For i As Integer = 1 To 4
            Select Case currentPhase
                Case 1
                    If phaseTimer <= greenTime Then
                        ' N/S is GREEN
                        SetTrafficLight(i, "N", LightState.Green)
                        SetTrafficLight(i, "S", LightState.Green)
                        SetTrafficLight(i, "E", LightState.Red)
                        SetTrafficLight(i, "W", LightState.Red)
                        SetPedestrianLight(i, "N", LightState.Green)
                        SetPedestrianLight(i, "S", LightState.Green)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    ElseIf phaseTimer <= greenTime + yellowTime Then
                        ' N/S is YELLOW
                        SetTrafficLight(i, "N", LightState.Yellow)
                        SetTrafficLight(i, "S", LightState.Yellow)
                        SetTrafficLight(i, "E", LightState.Red)
                        SetTrafficLight(i, "W", LightState.Red)
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    End If

                Case 2
                    If phaseTimer <= greenTime Then
                        ' E/W is GREEN
                        SetTrafficLight(i, "N", LightState.Red)
                        SetTrafficLight(i, "S", LightState.Red)
                        SetTrafficLight(i, "E", LightState.Green)
                        SetTrafficLight(i, "W", LightState.Green)
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Green)
                        SetPedestrianLight(i, "W", LightState.Green)
                    ElseIf phaseTimer <= greenTime + yellowTime Then
                        ' E/W is YELLOW
                        SetTrafficLight(i, "N", LightState.Red)
                        SetTrafficLight(i, "S", LightState.Red)
                        SetTrafficLight(i, "E", LightState.Yellow)
                        SetTrafficLight(i, "W", LightState.Yellow)
                        SetPedestrianLight(i, "N", LightState.Red)
                        SetPedestrianLight(i, "S", LightState.Red)
                        SetPedestrianLight(i, "E", LightState.Red)
                        SetPedestrianLight(i, "W", LightState.Red)
                    End If
            End Select
        Next
    End Sub

    ' ===== CHECK PEDESTRIAN REQUESTS =====
    Private Sub CheckPedestrianRequests()
        ' Only process one pedestrian request at a time
        For Each key In pedestrianRequests.Keys.ToList()
            If pedestrianRequests(key) And Not pedestrianActive(key) Then
                ' Mark as active immediately to prevent duplicate processing
                pedestrianActive(key) = True
                pedestrianRequests(key) = False

                ' Extract direction and intersection
                Dim direction As String = key.Substring(0, 1)
                Dim intersection As Integer = Integer.Parse(key.Substring(1))

                ' Store which pedestrian is crossing
                activePedestrianDirection = direction
                activePedestrianIntersection = intersection

                ' Enter pedestrian mode
                isInPedestrianMode = True
                pedestrianModeTimer = 0
                pedestrianPhase = 0
                phaseTimer = 0

                ' Turn perpendicular lights YELLOW at THIS INTERSECTION ONLY
                Select Case direction
                    Case "N", "S"
                        SetTrafficLight(intersection, "E", LightState.Yellow)
                        SetTrafficLight(intersection, "W", LightState.Yellow)
                        UpdateStatus($"🚶 INT{intersection} {direction}: E/W turning YELLOW...")

                    Case "E", "W"
                        SetTrafficLight(intersection, "N", LightState.Yellow)
                        SetTrafficLight(intersection, "S", LightState.Yellow)
                        UpdateStatus($"🚶 INT{intersection} {direction}: N/S turning YELLOW...")
                End Select

                ' Exit after processing the first request
                Exit For
            End If
        Next
    End Sub

    ' ===== ELAPSED TIME TIMER =====
    Private Sub ElapsedTime_Tick(sender As Object, e As EventArgs)
        If isRunning Then
            elapsedTime += 1
            lblTimer.Text = $"Elapsed: {elapsedTime}s"
        End If
    End Sub

    ' ===== PEDESTRIAN BUTTON CLICK HANDLER =====
    Private Sub PedestrianButton_Click(sender As Object, e As EventArgs)
        Try
            Dim btn As Button = CType(sender, Button)
            Dim buttonName As String = btn.Name

            If buttonName.Length < 4 Then Exit Sub

            ' Extract direction (position 3) and intersection (position 4)
            Dim direction As String = buttonName.Substring(3, 1)
            Dim intersectionStr As String = buttonName.Substring(4, 1)
            Dim key As String = $"{direction}{intersectionStr}"

            If pedestrianRequests.ContainsKey(key) Then
                ' Only set request if not already active
                If Not pedestrianActive(key) Then
                    pedestrianRequests(key) = True
                    btn.BackColor = Color.LightGreen
                    btn.Text = $"🚶 {direction} ✓"

                    UpdateStatus($"🚶 Pedestrian Button: {direction} at Intersection {intersectionStr}")

                    ' Reset button display after 2 seconds, but keep request active until processed
                    Dim tmr As New System.Windows.Forms.Timer()
                    tmr.Interval = 2000
                    AddHandler tmr.Tick, Sub()
                                             btn.BackColor = Color.LightYellow
                                             btn.Text = $"🚶 {direction}"
                                             tmr.Stop()
                                             tmr.Dispose()
                                         End Sub
                    tmr.Start()
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}")
        End Try
    End Sub

    ' ===== UPDATE STATUS LABEL =====
    Private Sub UpdateStatus(message As String)
        Try
            If lblStatus.InvokeRequired Then
                lblStatus.Invoke(Sub() lblStatus.Text = $"Status: {message}")
            Else
                lblStatus.Text = $"Status: {message}"
            End If
        Catch
        End Try
    End Sub

End Class