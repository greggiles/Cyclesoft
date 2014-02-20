var start = function () {
    //pageone
    var workoutName = document.getElementById('workoutName');
    var segmentName = document.getElementById('segmentName');
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
    var powerIndNext1RampUp = document.getElementById('powerIndNext1RampUp');
    var powerIndNext2RampUp = document.getElementById('powerIndNext2RampUp');
    var powerIndNext3RampUp = document.getElementById('powerIndNext3RampUp');
    var powerIndNext4RampUp = document.getElementById('powerIndNext4RampUp');
    var powerIndNext5RampUp = document.getElementById('powerIndNext5RampUp');
    var powerIndNext6RampUp = document.getElementById('powerIndNext6RampUp');
    var powerIndNext7RampUp = document.getElementById('powerIndNext7RampUp');
    var powerIndNext8RampUp = document.getElementById('powerIndNext8RampUp');
    var powerIndNext9RampUp = document.getElementById('powerIndNext9RampUp');
    var powerIndNext1RampDown = document.getElementById('powerIndNext1RampDown');
    var powerIndNext2RampDown = document.getElementById('powerIndNext2RampDown');
    var powerIndNext3RampDown = document.getElementById('powerIndNext3RampDown');
    var powerIndNext4RampDown = document.getElementById('powerIndNext4RampDown');
    var powerIndNext5RampDown = document.getElementById('powerIndNext5RampDown');
    var powerIndNext6RampDown = document.getElementById('powerIndNext6RampDown');
    var powerIndNext7RampDown = document.getElementById('powerIndNext7RampDown');
    var powerIndNext8RampDown = document.getElementById('powerIndNext8RampDown');
    var powerIndNext9RampDown = document.getElementById('powerIndNext9RampDown');
    var segTime = document.getElementById('segTime');
    var totTime = document.getElementById('totTime');

	var powerBarContainer = document.getElementById('powerBarContainer');
	var powerBar = document.getElementById('powerBar');
	var powerBarTarget = document.getElementById('powerBarTarget');
	var powerBarRange = document.getElementById('powerBarRange');
	var powerBarTargetNext = document.getElementById('powerBarTargetNext');
	var powerBarPointDisplay = document.getElementById('powerBarPoints');

	var cadBarContainer = document.getElementById('cadBarContainer');
	var cadBar = document.getElementById('cadBar');
	var cadBarTarget = document.getElementById('cadBarTarget');
	var cadBarRange = document.getElementById('cadBarRange');
	var cadBarTargetNext = document.getElementById('cadBarTargetNext');
	
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

    //inc.innerHTML += "connecting to server ..<br/>";



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
            workoutName.innerHTML = jsonData.title;
            segments = jsonData.segments;
        };

        selectedUser = Number(getQueryVariable("id") - 1);
        if (selectedUser < 0)
            selectedUser = 0;


        if (jsonData.wEA) {
            userName.innerHTML = jsonData.uEAs[selectedUser].name;
            var seg = jsonData.wEA.currentSegment;
            if (seg < 0) seg = 0;

            var currentEffort = segments[seg].effort; 
            if (jsonData.wEA.alternateTarget > 0)
                currentEffort = jsonData.wEA.alternateTarget;

            segmentName.innerHTML = segments[seg].segmentName;

            powerAct.innerHTML = jsonData.uEAs[selectedUser].instPwr;

            powerTar.innerHTML = (currentEffort * jsonData.uEAs[selectedUser].ftp).toFixed(0);
            powerAct.style.backgroundColor = "white";
			powerBarContainer.style.backgroundColor = "white";
            if (jsonData.uEAs[selectedUser].instPwr >= ((currentEffort + segments[seg].ptsPlus) * jsonData.uEAs[selectedUser].ftp))
            {
				powerAct.style.backgroundColor = "white";
				powerBarContainer.style.backgroundColor = "white";
            }
			else if (jsonData.uEAs[selectedUser].instPwr >= (currentEffort * jsonData.uEAs[selectedUser].ftp))
            {
				powerAct.style.backgroundColor = "lime";
				powerBarContainer.style.backgroundColor = "lime";
            }
			else if (jsonData.uEAs[selectedUser].instPwr >= ((currentEffort - segments[seg].ptsMinus) * jsonData.uEAs[selectedUser].ftp))
            {
				powerAct.style.backgroundColor = "yellow";
				powerBarContainer.style.backgroundColor = "yellow";
			}

            powerLast.innerHTML = jsonData.uEAs[selectedUser].lastAvgPwr;

			powerBar.style.width = (~~((jsonData.uEAs[selectedUser].instPwr / jsonData.uEAs[selectedUser].ftp) * 50)).toString() + "%";
			powerBarTarget.style.left = (currentEffort * 50).toString() + "%";
			powerBarRange.style.left = ((currentEffort - segments[seg].ptsMinus) * 50).toString() + "%";
			powerBarRange.style.width = ((segments[seg].ptsPlus + segments[seg].ptsMinus) * 50).toString() + "%";
            if ((jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS) < 10000) {
				powerBarTargetNext.style.left = (segments[seg+1].effort * 50).toString() + "%";
				powerBarTargetNext.style.visibility = "visible";
			}
			else powerBarTargetNext.style.visibility = "hidden";

			powerBarPointDisplay.innerHTML = jsonData.uEAs[selectedUser].points;
			

            cadAct.innerHTML = jsonData.uEAs[selectedUser].cad;

            cadAct.style.backgroundColor = "white";
            cadBarContainer.style.backgroundColor = "white";
            if (jsonData.uEAs[selectedUser].cad > (segments[seg].cadTarget + segments[seg].ptsCadPlus))
            {
				cadAct.style.backgroundColor = "white";
				cadBarContainer.style.backgroundColor = "white";
            }
			else if (jsonData.uEAs[selectedUser].cad >= (segments[seg].cadTarget))
            {
				cadAct.style.backgroundColor = "lime";
				cadBarContainer.style.backgroundColor = "lime";
            }
			else if (jsonData.uEAs[selectedUser].cad >= (segments[seg].cadTarget - segments[seg].ptsCadMinus))
            {
				cadAct.style.backgroundColor = "yellow";
				cadBarContainer.style.backgroundColor = "yellow";
            }

            cadTar.innerHTML = segments[seg].cadTarget;
            heartRate.innerHTML = jsonData.uEAs[selectedUser].hr;

            cadBar.style.width = (~~((jsonData.uEAs[selectedUser].cad / 150) * 100)).toString() + "%";
            cadBarTarget.style.left = (0.6666*segments[seg].cadTarget).toString() + "%";
            cadBarRange.style.left = (0.6666 * (segments[seg].cadTarget - segments[seg].ptsCadMinus)).toString().toString() + "%";
            cadBarRange.style.width = (0.6666*(segments[seg].ptsCadPlus + segments[seg].ptsCadMinus)).toString() + "%";
            if ((jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS) < 10000) {
                cadBarTargetNext.style.left = (0.6666 * segments[seg+1].cadTarget).toString() + "%";
                cadBarTargetNext.style.visibility = "visible";
            }
            else cadBarTargetNext.style.visibility = "hidden";


			

            totTime.innerHTML = msToTime(jsonData.wEA.workoutCurrentMS);
            segTime.innerHTML = msToTime(999 + jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS);

            powerIndNext1.style.width = (~~(jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS) / 500).toString() + "px";


            if (segments[seg].type == 'OverUnder')
            {
                powerIndNext1.style.height = (currentEffort * 50).toString() + "%";
                powerIndNext1RampUp.style.visibility = "hidden";
                powerIndNext1RampDown.style.visibility = "hidden";
            }
            else if  (segments[seg].type == 'ramp') 
            {
                if (currentEffort < segments[seg].effortFinish)
                {
                    // ramp up condition
                    powerIndNext1.style.height = (currentEffort * 50).toString() + "%";
                    powerIndNext1RampDown.style.visibility = "hidden";
                    powerIndNext1RampUp.style.visibility = "visible";
                    powerIndNext1RampUp.style.borderLeftWidth = (~~(jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS) / 500 + 1).toString() + "px";;
                    powerIndNext1RampUp.style.borderBottomWidth = ~~(40*(segments[seg].effortFinish - currentEffort)+1).toString() + "px";
                    powerIndNext1RampUp.style.top = ((2-segments[seg].effortFinish) * 50).toString()+"%"
                }
                else
                {
                    //ramp down condition
                    powerIndNext1.style.height = (segments[seg].effortFinish * 50).toString() + "%";
                    powerIndNext1RampUp.style.visibility = "hidden";
                    powerIndNext1RampDown.style.visibility = "visible";
                    powerIndNext1RampDown.style.borderRightWidth = (~~(jsonData.wEA.segmentTotalMS - jsonData.wEA.segmentCurrentMS) / 500 + 1).toString() + "px";;
                    powerIndNext1RampDown.style.borderBottomWidth = ~~(40 * (currentEffort - segments[seg].effortFinish) + 1).toString() + "px";
                    powerIndNext1RampDown.style.top = ((2 - currentEffort) * 50).toString() + "%"

                }

            }
            else
            {
                powerIndNext1.style.height = (currentEffort * 50).toString() + "%";
                powerIndNext1RampUp.style.visibility = "hidden";
                powerIndNext1RampDown.style.visibility = "hidden";
            }


            powerIndCur.style.height = (~~((jsonData.uEAs[selectedUser].instPwr / jsonData.uEAs[selectedUser].ftp) * 50)).toString() + "%";

            //if (seg != segLast) {
                segLast = seg;
                updateInd(powerIndNext2, powerIndNext2RampUp, powerIndNext2RampDown, segments[seg + 1]);
                updateInd(powerIndNext3, powerIndNext3RampUp, powerIndNext3RampDown, segments[seg + 2]);
                updateInd(powerIndNext4, powerIndNext4RampUp, powerIndNext4RampDown, segments[seg + 3]);
                updateInd(powerIndNext5, powerIndNext5RampUp, powerIndNext5RampDown, segments[seg + 4]);
                updateInd(powerIndNext6, powerIndNext6RampUp, powerIndNext6RampDown, segments[seg + 5]);
                updateInd(powerIndNext7, powerIndNext7RampUp, powerIndNext7RampDown, segments[seg + 6]);
                updateInd(powerIndNext8, powerIndNext8RampUp, powerIndNext8RampDown, segments[seg + 7]);
                updateInd(powerIndNext9, powerIndNext9RampUp, powerIndNext9RampDown, segments[seg + 8]);
            //};


            //inc1.innerHTML = JSON.stringify(jsonData.wEA, null, "\t");
            /*inc2.innerHTML = "users:</br>" 
            */

            if ( userLast != jsonData.uEAs.length)
            {
                userLast != jsonData.uEAs.length;
                userSelect.innerHTML = "<option value=\"0\">Select</option>";
                points.innerHTML = "";
                for (var i in jsonData.uEAs) {
                    //<option value="standard">Standard: 7 day</option>
                    userSelect.innerHTML += "<option value=\"" + (Number(i) + 1) + "\">" + jsonData.uEAs[i].name + "</option>";
                    points.innerHTML += "<div class=\"gTime\">";
                    points.innerHTML += "<div class=\"gTimeLabel\">" + jsonData.uEAs[i].name + "</div>";
                    points.innerHTML += "<div class=\"gSpace\"></div>";
                    points.innerHTML += "<div class=\"gTimeTime\">" + jsonData.uEAs[i].points + "</div>";
                    points.innerHTML += "</div>";
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

function updateInd(base, RampUp, RampDown, seg)
{
    if (seg == null) {
        base.style.visibility = "hidden";
        RampUp.style.visibility = "hidden";
        RampDown.style.visibility = "hidden";
        return;
    }

    base.style.width = (~~(seg.length * 2)).toString() + "px";

    if (seg.type == 'overunder')
    {
        base.style.height = (seg.effort * 50).toString() + "%";
        RampDown.style.visibility = "hidden";
        RampUp.style.visibility = "hidden";
    }
    else if  (seg.type == 'steady') 
    {
        base.style.height = (seg.effort * 50).toString() + "%";
        RampDown.style.visibility = "hidden";
        RampUp.style.visibility = "hidden";
    }
    else if  (seg.type == 'ramp') 
    {
        if (seg.effort < seg.effortFinish)
        {
            // ramp up condition
            base.style.height = (seg.effort * 50).toString() + "%";
            RampDown.style.visibility = "hidden";
            RampUp.style.visibility = "visible";
            
            RampUp.style.borderLeftWidth = (~~(seg.length * 2)).toString() + "px";
            RampUp.style.borderBottomWidth = ~~(40*(seg.effortFinish - seg.effort)+1).toString() + "px";
            RampUp.style.top = ((2 - seg.effortFinish) * 50).toString() + "%"
            RampUp.style.left = (base.offsetLeft+1).toString() + "px";
        }
        else
        {
            //ramp down condition
            base.style.height = (seg.effortFinish * 50).toString() + "%";
            RampDown.style.visibility = "visible";
            RampUp.style.visibility = "hidden";

            RampDown.style.borderRightWidth = (~~(seg.length * 2)).toString() + "px";
            RampDown.style.borderBottomWidth = ~~(40 * (seg.effort - seg.effortFinish) + 1).toString() + "px";
            RampDown.style.top = ((2 - seg.effort) * 50).toString() + "%"
            RampDown.style.left = (base.offsetLeft-1).toString() + "px";

        }
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
