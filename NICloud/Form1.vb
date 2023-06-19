Imports System.IO
Imports System.Net
Imports System.Windows.Forms

Public Class Form1
    Private ftpServer As String = "ftp://exemple.com/"
    Private ftpUsername As String = "username"
    Private ftpPassword As String = "password"
    Private limit As Long = 10 'GB Limit

    Private ftpDirectory As String ' FTP Folder

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim openFileDialog As New OpenFileDialog()
        openFileDialog.Filter = "All Files (*.*)|*.*"
        openFileDialog.RestoreDirectory = True

        If openFileDialog.ShowDialog() = DialogResult.OK Then

            Dim fileSize As Long = New FileInfo(openFileDialog.FileName).Length
            Dim maxSize As Long = 1024 * 1024 * 1024

            If fileSize > maxSize Then
                MessageBox.Show("This file exceeds the limit (1GB)", "Error")
            Else

                Dim fileExtension As String = Path.GetExtension(openFileDialog.FileName)
                If fileExtension = ".exe" OrElse fileExtension = ".htm" OrElse fileExtension = ".html" OrElse fileExtension = ".php" OrElse fileExtension = ".css" OrElse fileExtension = ".py" OrElse fileExtension = ".pyw" Then
                    MessageBox.Show("The .exe .htm .html .php .css .py .pyw files is not allowed ", "Error")
                Else

                    UploadFileToFtp(openFileDialog.FileName)
                    MessageBox.Show("The file has been sent")
                End If
            End If
        End If
    End Sub

    Private Sub UploadFileToFtp(filePath As String)
        Dim fileName As String = Path.GetFileName(filePath)
        Dim request As FtpWebRequest = DirectCast(WebRequest.Create(Path.Combine(ftpDirectory, fileName)), FtpWebRequest)
        request.Method = WebRequestMethods.Ftp.UploadFile
        request.Credentials = New NetworkCredential(ftpUsername, ftpPassword)

        Dim fileContents As Byte() = File.ReadAllBytes(filePath)
        request.ContentLength = fileContents.Length

        Using requestStream As Stream = request.GetRequestStream()
            requestStream.Write(fileContents, 0, fileContents.Length)
            requestStream.Close()
        End Using
    End Sub

    Private Sub RefreshFileList()
        ListBox1.Items.Clear()

        Dim request As FtpWebRequest = WebRequest.Create(ftpDirectory)
        request.Method = WebRequestMethods.Ftp.ListDirectory

        request.Credentials = New NetworkCredential(ftpUsername, ftpPassword)

        Dim response As FtpWebResponse = request.GetResponse()
        Dim responseStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(responseStream)

        While Not reader.EndOfStream
            ListBox1.Items.Add(reader.ReadLine())
        End While

        reader.Close()
        response.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Try
            If ListBox1.SelectedItem IsNot Nothing Then
                Dim selectedFile As String = ListBox1.SelectedItem.ToString()
                Dim desktopPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                Dim localFilePath As String = Path.Combine(desktopPath, selectedFile)

                Dim request As FtpWebRequest = WebRequest.Create(Path.Combine(ftpDirectory, selectedFile))
                request.Method = WebRequestMethods.Ftp.DownloadFile

                request.Credentials = New NetworkCredential(ftpUsername, ftpPassword)

                Dim response As FtpWebResponse = request.GetResponse()
                Dim responseStream As Stream = response.GetResponseStream()
                Dim fileStream As New FileStream(localFilePath, FileMode.Create)

                responseStream.CopyTo(fileStream)

                fileStream.Close()
                response.Close()


                DeleteFileFromFtp(selectedFile)
                MessageBox.Show("The File has been downloaded")
            Else
                MessageBox.Show("Please select a file")
            End If
        Catch
            MessageBox.Show("Error, Please Update", "Error")
        End Try

    End Sub

    Private Sub DeleteFileFromFtp(fileName As String)
        Dim request As FtpWebRequest = WebRequest.Create(Path.Combine(ftpDirectory, fileName))
        request.Method = WebRequestMethods.Ftp.DeleteFile
        request.Credentials = New NetworkCredential(ftpUsername, ftpPassword)

        Dim response As FtpWebResponse = request.GetResponse()
        response.Close()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CreateFtpDirectory()
        RefreshFileList()
    End Sub

    Private Sub CreateFtpDirectory()
        Dim random As New Random()
        Dim folderName As String = GenerateRandomName(20)
        ftpDirectory = ftpServer & folderName

        Dim request As FtpWebRequest = WebRequest.Create(ftpDirectory)
        request.Method = WebRequestMethods.Ftp.MakeDirectory
        request.Credentials = New NetworkCredential(ftpUsername, ftpPassword)

        Try
            Dim response As FtpWebResponse = request.GetResponse()
            response.Close()
        Catch ex As WebException

            Me.Hide()
            MessageBox.Show("Error")
            Me.Close()
        End Try
    End Sub

    Private Sub DeleteFtpDirectory()
        Dim request As FtpWebRequest = WebRequest.Create(ftpDirectory)
        request.Method = WebRequestMethods.Ftp.RemoveDirectory
        request.Credentials = New NetworkCredential(ftpUsername, ftpPassword)

        Dim response As FtpWebResponse = Nothing

        Try
            response = request.GetResponse()
            response.Close()
        Catch ex As WebException
            Me.Close()
        End Try
    End Sub

    Private Sub CheckTotalFileSize()
        Dim request As FtpWebRequest = WebRequest.Create(ftpServer)
        request.Method = WebRequestMethods.Ftp.ListDirectoryDetails
        request.Credentials = New NetworkCredential(ftpUsername, ftpPassword)

        Dim response As FtpWebResponse = request.GetResponse()
        Dim responseStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(responseStream)

        Dim totalSize As Long = 0

        While Not reader.EndOfStream
            Dim line As String = reader.ReadLine()
            Dim lineSplit As String() = line.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)

            Dim fileSize As Long = 0
            Long.TryParse(lineSplit(4), fileSize)

            totalSize += fileSize
        End While

        reader.Close()
        response.Close()

        Dim limitSizeInBytes As Long = limit * 1024 * 1024 * 1024

        If totalSize > limitSizeInBytes Then
            Me.Close()
        End If
    End Sub

    Private Function GenerateRandomName(length As Integer) As String
        Const chars As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        Dim random As New Random()
        Return New String(Enumerable.Repeat(chars, length).Select(Function(s) s(random.Next(s.Length))).ToArray())
    End Function

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        RefreshFileList()
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        Me.FormBorderStyle = FormBorderStyle.None
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Try
            DeleteFtpDirectory()
        Catch
            Me.Close()
        End Try
        Me.Close()
    End Sub
End Class
