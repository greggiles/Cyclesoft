How to Start the web services:

1 - Add the URL ACL:
	netsh http add urlacl url=http://*:8080/ user=everyone
	
2 - Add the Firewall Rule:
	netsh advfirewall firewall add rule name="WorkoutTracker" dir=in protocol=tcp localport=8080 profile=private remoteip=localsubnet action=allow
	
3 - Start IIS Express:
	"c:\Program Files\IIS Express\iisexpress.exe" /config:./applicationhost.config /trace:error
	
Config includes modifications:

            <site name="WorkoutTracker" id="1" serverAutoStart="true">
                <application path="/">
                    <virtualDirectory path="/" physicalPath="c:\Users\ggiles\Documents\GitHub\CycleSoft\CycleSoft" />
                </application>
                <bindings>
                    <binding protocol="http" bindingInformation=":8080:" />
                </bindings>

				
				