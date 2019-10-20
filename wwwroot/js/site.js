// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var blocked = false;

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

            var apiLink = '/api/DownloadAPI/mp3/';

            if (isVideo) {
                apiLink = '/api/DownloadAPI/mp4/';
            }

            var url = document.getElementById('url_input').value;

            var myHeaders = new Headers();
            myHeaders.append('download_url', url);

            var myInit = {
                method: 'GET',
                headers: myHeaders,
                mode: 'cors',
                cache: 'default'
            };


            PleaseWait();
            var response = await fetch(window.location.origin + apiLink, myInit);
            var json = await response.json();

            if (json.Success) {
                Done();
                downloadURI(json.DownloadPath);
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
    var element = document.getElementById("main");
    element.classList.add("loading");
}

function Done() {
    var element = document.getElementById("main");
    element.classList.remove("loading");
    blocked = false;
}

function Fail() {
    var element = document.getElementById("main");
    element.classList.remove("loading");
    element.classList.add("failed");
}