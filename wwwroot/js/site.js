// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var blocked = false;
var downloadAsVideo = true;
var form = document.getElementsByTagName("form")[0];

form.onsubmit = function (event) {
    Download(downloadAsVideo);
    return false;
};

function downloadAudio()
{
    downloadAsVideo = false;
    document.getElementById('download-video').click();
    downloadAsVideo = true;
}

function downloadURI(url) {
    try {
        let a = document.createElement('a');
        a.href = url;
        a.download = url.split('/').pop();
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }
    catch (err)
    {
        console.log(err);
    }
}

async function Download(isVideo, isUser = true) {
    try {
        if (!isUser || !blocked) {
            blocked = true;

            var apiLink = '/api/DownloadAPI/v1.0/audio/';

            if (isVideo) {
                apiLink = '/api/DownloadAPI/v1.0/video/';
            }

            var url = document.getElementById('url_input').value;

            var headers = new Headers();
            headers.append('Accept', 'application/json');
            headers.append('Content-Type', 'application/json');

            var request_body = {
                "DownloadURL": url,
                "CallSource": "SASRip.cf"
            };

            var init = {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(request_body)
            };


            PleaseWait();
            var response = await fetch(window.location.origin + apiLink, init);
            var json = await response.json();

            if (json.success) {
                Done();
                downloadURI(json.downloadPath);
            }
            else if (json.Status === "file_processing") {
                // If file is processing from another request, check every 5 seconds.
                setTimeout(function () { Download(isVideo, false); }, 5000);
            }
            else {
                Fail();
            }
        }
    }
    catch (error) {
        Fail();
    }
}

function PleaseWait() {
    var element = document.getElementsByTagName("main")[0];
    element.classList.add("loading");
}

function Done() {
    var element = document.getElementsByTagName("main")[0];
    element.classList.remove("loading");
    blocked = false;
}

function Fail() {
    var element = document.getElementsByTagName("main")[0];
    element.classList.remove("loading");
    element.classList.add("failed");
}