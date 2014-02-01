var start = function () {
    //pageone
    var workoutName = document.getElementById('workoutName');
    var userName = document.getElementById('userName');
    var powerAct = document.getElementById('powerAct');
    var powerTar = document.getElementById('powerTar');
    var powerLast = document.getElementById('powerLast');
    var cadAct = document.getElementById('cadAct');
    var cadTar = document.getElementById('cadTar');
    var heartRate = document.getElementById('heartRate');
    var powerIndCur = document.getElementById('powerIndCur');
    var powerIndNext1 = document.getElementById('powerIndNext1');
    var powerIndNext2 = document.getElementById('powerIndNext2');
    var powerIndNext3 = document.getElementById('powerIndNext3');
    var powerIndNext4 = document.getElementById('powerIndNext4');
    var powerIndNext5 = document.getElementById('powerIndNext5');
    var powerIndNext6 = document.getElementById('powerIndNext6');
    var powerIndNext7 = document.getElementById('powerIndNext7');
    var powerIndNext8 = document.getElementById('powerIndNext8');
    var powerIndNext9 = document.getElementById('powerIndNext9');
    var segTime = document.getElementById('segTime');
    var totTime = document.getElementById('totTime');


    //pagetwo
    var userSelect = document.getElementById('userSelect');



    //debug page
    var inc = document.getElementById('incoming');
    var inc1 = document.getElementById('WorkoutStatus');
    var inc2 = document.getElementById('UserStatus');
    var inc3 = document.getElementById('WorkoutDef');

    var title = document.getElementById('title');
    var powerLabel = document.getElementById('powerLabel');

    var time = document.getElementById('Time');
    var wsImpl = window.WebSocket || window.MozWebSocket;
    var form = document.getElementById('sendForm');
    var input = document.getElementById('sendText');

    var segments;
    var selectedUser = 0;
    var segLast = -1;   // segment last scan
    var userLast = 0;   // how many users? 

    inc.innerHTML += "connecting to server ..<br/>";



    // create a new websocket and connect
    var ip = 'ws://' + location.hostname + ':8181/';

    window.ws = new wsImpl(ip);

    var homeadd = document.getElementById('homeadd');
    homeadd.innerHTML = "checking user Agent ...";
    if (window.navigator.userAgent.indexOf('iPhone') != -1) {
        if (window.navigator.standalone == true) {
            //initialize();
            homeadd.innerHTML = "Yep1";
        } else {
            homeadd.innerHTML = "Add to Home Screen";
        }
    }
    else
        homeadd.innerHTML = "not iPhone";

    // when data is comming from the server, this metod is called
    ws.onmessage = function (evt) {
        //      inc.innerHTML += evt.data + '<br/>';
        //  inc.innerHTML = evt.data + '<br/>';

        var jsonData = JSON.parse(evt.data);

        if (jsonData.title) {
            //pageone
            workoutName.innerHTML = jsonData.title + " - ";
            segments = jsonData.segments;
        };

        selectedUser = Number(getQueryVariable("id") - 1);
        if (selectedUser < 0)
            selectedUser = 0;


        if (jsonData.wEA) {
            userName.innerHTML = jsonData.uEAs[selectedUser].name;
            var seg = jsonData.wEA.currentSegment;
            if (seg < 0) seg = 0;

            powerAct.innerHTML = jsonData.uEAs[selectedUser].instPwr;
            powerTar.innerHTML = (segments[seg].effort * jsonData.uEAs[selectedUser].ftp).toFixed(0);
            powerLast.innerHTML = jsonData.uEAs[selectedUser].lastAvgPwr;

            cadAct.innerHTML = jsonData.uEAs[selectedUser].cad;
            cadTar.innerHTML = segments[seg].cadTarget;
            heartRate.innerHTML = jsonData.uEAs[selectedUser].hr;

            powerIndCur.style.height = (~~((jsonData.uEAs[selectedUser].instPwr / jsonData.uEAs[selectedUser].ftp) * 50)).toString() + "%";


            powerIndNext1.style.width = (~~(jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS) / 500).toString() + "px";
            powerIndNext1.style.height = (segments[seg].effort * 50).toString() + "%";

            if (seg != segLast) {
                segLast = seg;
                powerIndNext2.style.height = (segments[seg + 1].effort * 50).toString() + "%";
                powerIndNext2.style.width = (~~(segments[seg + 1].length * 2)).toString() + "px";
                powerIndNext3.style.height = (segments[seg + 2].effort * 50).toString() + "%";
                powerIndNext3.style.width = (~~(segments[seg + 2].length * 2)).toString() + "px";
                powerIndNext4.style.height = (segments[seg + 3].effort * 50).toString() + "%";
                powerIndNext4.style.width = (~~(segments[seg + 3].length * 2)).toString() + "px";
                powerIndNext5.style.height = (segments[seg + 4].effort * 50).toString() + "%";
                powerIndNext5.style.width = (~~(segments[seg + 4].length * 2)).toString() + "px";
                powerIndNext6.style.height = (segments[seg + 5].effort * 50).toString() + "%";
                powerIndNext6.style.width = (~~(segments[seg + 5].length * 2)).toString() + "px";
                powerIndNext7.style.height = (segments[seg + 6].effort * 50).toString() + "%";
                powerIndNext7.style.width = (~~(segments[seg + 6].length * 2)).toString() + "px";
                powerIndNext8.style.height = (segments[seg + 7].effort * 50).toString() + "%";
                powerIndNext8.style.width = (~~(segments[seg + 7].length * 2)).toString() + "px";
                powerIndNext9.style.height = (segments[seg + 8].effort * 50).toString() + "%";
                powerIndNext9.style.width = (~~(segments[seg + 8].length * 2)).toString() + "px";
            };

            totTime.innerHTML = msToTime(jsonData.wEA.workoutCurrentMS);
            segTime.innerHTML = msToTime(999 + jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS);



            //inc1.innerHTML = JSON.stringify(jsonData.wEA, null, "\t");
            /*inc2.innerHTML = "users:</br>" 
            */

            if ( userLast != jsonData.uEAs.length)
            {
                userLast != jsonData.uEAs.length;
                userSelect.innerHTML = "<option value=\"0\">Select</option>";
                for (var i in jsonData.uEAs) {
                    //<option value="standard">Standard: 7 day</option>
                    userSelect.innerHTML += "<option value=\"" + (Number(i) + 1) + "\">" + jsonData.uEAs[i].name + "</option>";
                }

            }

            /*
*/

        };

    };

    // when the connection is established, this method is called
    ws.onopen = function () {
        inc.innerHTML += '.. connection open<br/>';
    };

    // when the connection is closed, this method is called
    ws.onclose = function () {
        inc.innerHTML += '.. connection closed<br/>';
    }

}

function msToTime(s) {
    var ms = s % 1000;
    s = (s - ms) / 1000;
    var secs = s % 60;
    s = (s - secs) / 60;
    var mins = s % 60;
    var hrs = (s - mins) / 60;

    return hrs + ':' + pad(mins) + ':' + pad(secs);
    //    return hrs + ':' + pad(mins) + ':' + pad(secs) + '.' + ms;
}

function pad(n) {
    return (n < 10) ? ("0" + n) : n;
}


function getQueryVariable(variable) {
    var query = window.location.search.substring(1);
    var vars = query.split("&");
    for (var i = 0; i < vars.length; i++) {
        var pair = vars[i].split("=");
        if (pair[0] == variable) { return pair[1]; }
    }
    return (false);
}
window.onload = start;
