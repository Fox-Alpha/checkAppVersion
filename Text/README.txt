checkAppVersion
===============
checkAppVersion stellt ein Tool dar mit dem es möglich ist eine Version eines laufenden Prozesses gegen eine als Parameter Übergebene Version zu vergleichen. 
Da Ergebnis kann z.B. in der Monitoring Umgebung Nagio verwendet werden um sicherzustellen das nur eine bestimmte Version verwendet wird.

Der Aufruf kann z.B. über den Nagios Windows Client NSCLient++ erfolgen

Es können folgende Parameter übergeben werden.

Parameter
=========
-process =	Der Name des Prozesses der geprüft werden. Beispielsweise explorer. Hierbei  muss die Endung, also .exe, weggelassen werden. Dies muss mindestens angeben sein.
			Macht auch sonst keinen sinn.

-version =	Hiermit wird die Version übergeben die laufen soll. Diese kann beliebig sein z.B. 1.0.10.2
			Der Aufbau der Version kann beliebig sein muss aber bei mehreren Stellen mit einem '.' (Punkt) getrennt sein.
			Wird die Version nicht mit angegeben, wird keine voraussetzung geprüft und nur die verwendete Version ermittelt und ausgegeben.
			Dies kann bei nicht kritischen Versionsnummern verwendet werden.

-compare =	Diese Option gibt an wie die angegebene Version verglichen werden soll.
			
			-TEXT = Dies ein einfacher Text Vergleich der Version und sollte verwendet werden, wenn die Version nicht nummerische Werte wie a oder alpha/beta enthält
			-Major,Minor,Build,Private = Diese Angaben beziehen sich auf die am meisten verwendeten vierteiligen Versionsangaben und stellen jeweils die einzelnen Teile
					in dieser Reihenfolge dar. Dieser Angaben können beliebig kombiniert werden.
					Will man alle Werte vergleichen kann man statt alle Angaben einzeln anzugeben auch den Wert 'ALL' verwenden.
			-All =	Dieser Wert kombiniert alle Angaben der gängigen Versionsangabe. Bei mehr als vier teilen kann man hier auch mehr vergleichen.

			Wird dieser Parameter angegeben muss auch eine Version übergeben werden. Sonst gibt es ja nichts zum vergleichen.


Einbinden im Nagios
==================
TODO


Einbinden in NSClient++
=======================
TODO