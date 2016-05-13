checkAppVersion
===============
checkAppVersion stellt ein Tool dar mit dem es möglich ist eine Version eines laufenden Prozesses gegen eine als Parameter Übergebene Version zu vergleichen. 
Da Ergebnis kann z.B. in der Monitoring Umgebung Nagio verwendet werden um sicherzustellen das nur eine bestimmte Version verwendet wird.

Der Aufruf kann z.B. über den Nagios Windows Client NSCLient++ erfolgen

Es können folgende Parameter übergeben werden.

Parameter
=========
-process = 	[Names des Prozesses ohne Dateiendung]
			Der Name des Prozesses der geprüft werden. Beispielsweise explorer. Hierbei  muss die Endung, also .exe, weggelassen werden. Dies muss mindestens angeben sein.
			Macht auch sonst keinen sinn.

-version =	[Version die vorhanden sein soll]
			Hiermit wird die Version übergeben die laufen soll. Diese kann beliebig sein z.B. 1.0.10.2
			Der Aufbau der Version kann beliebig sein muss aber bei mehreren Stellen mit einem '.' (Punkt) getrennt sein.
			Wird die Version nicht mit angegeben, wird keine voraussetzung geprüft und nur die verwendete Version ermittelt und ausgegeben.
			Dies kann bei nicht kritischen Versionsnummern verwendet werden.

-compare =	Diese Option gibt an wie die angegebene Version verglichen werden soll.
			
			-TEXT = Dies ist ein einfacher Text Vergleich der Version und sollte verwendet werden, wenn die Version nicht nummerische Werte wie a oder alpha/beta enthält
					TEXT wird auch verwendet, wenn keine Compare Option angegeben ist.
			-Major,Minor,Build,Private = Diese Angaben beziehen sich auf die am meisten verwendeten vierteiligen Versionsangaben und stellen jeweils die einzelnen Teile
					in dieser Reihenfolge dar. Diese Angaben können beliebig kombiniert werden.
					Will man alle Werte vergleichen kann man statt alle Angaben einzeln anzugeben auch den Wert 'ALL' verwenden.
			-All =	Dieser Wert kombiniert alle Angaben der gängigen Versionsangabe. Bei mehr als vier teilen kann man hier auch mehr vergleichen.

					Wird dieser Parameter angegeben muss auch eine Version übergeben werden. Sonst gibt es ja nichts zum vergleichen.

Voraussetzungen
===============
.NET FrameWork 4.0 oder höher


Einbinden im Nagios
==================

Aufruf über check_nrpe
Command: check_nrpe -n -H HH-ws-sd-003 -c alias_check_applicationversion -a '-process=JM4 -version=1.8.0.70 -compare=all'

####
#	Test zum Check JM4 / AM5 Versionen
####
define service{
	name					generic-service-nscp-versionchk
	service_description		Anwendungs Versionsabgleich
	display_name			Anwendungs Versionsabgleich
	is_volatile				0
	max_check_attempts		5
	normal_check_interval	1
	retry_check_interval	1
	active_checks_enabled	1
	passive_checks_enabled	0
	check_period			24x7
	notification_interval	0
	notification_period		workhours
	notification_options	u,c,r
	notifications_enabled	1
	# contact_groups			ServiceDesk
	register				0
}

define hostgroup{
	hostgroup_name			hstgrp_test_versionchk_jm4
	alias					Nagios-TestHosts-VersionChk-JM4
	notes					Testgruppe fuer Anwendungs Versionsabgleich
	members					HH-WS-SD-003,,HH-VS-JM-003,HH-VS-JM-004,HH-VS-JM-005,HH-VS-JM-007,HH-VS-JM-008
}
define hostgroup{
	hostgroup_name			hstgrp_test_versionchk_am5
	alias					Nagios-TestHosts-VersionChk-AM5
	notes					Testgruppe fuer Anwendungs Versionsabgleich
	members					HH-WS-SD-003,HH-VS-JM-001,HH-VS-JM-002,HH-VS-JM-004,HH-VS-JM-006
}
define servicegroup{
	servicegroup_name		svcgrp_versioncheck
	alias					Versions Check AM5 / JM4
	notes					Versions abgleich
}

define service{
	name					svc_am5_version_check
	display_name			AM5 Versions Check
	check_command			nrpe_alias_check_version_am5
	use						generic-service-nscp-versionchk
	servicegroups			svcgrp_versioncheck
	service_description		Versions abgleich für AM5
	normal_check_interval	1
	notifications_enabled	1
	contact_groups			ServiceDesk,nagiostestgroup
	hostgroup_name			hstgrp_test_versionchk_am5
	#hostgroup_name			Jobmanager-Speicher-Monitoring
	#host_name
}

define service{
	name					svc_jm4_version_check
	display_name			JM4 Versions Check
	check_command			nrpe_alias_check_version_jm4
	use						generic-service-nscp-versionchk
	servicegroups			svcgrp_versioncheck
	service_description		Versions abgleich für JM4
	normal_check_interval	1
	notifications_enabled	1
	contact_groups			ServiceDesk,nagiostestgroup
	hostgroup_name			hstgrp_test_versionchk_jm4
	#hostgroup_name			Jobmanager-Speicher-Monitoring
	#host_name
}

define command{
	command_name			nrpe_alias_check_version_jm4
	command_line			$USER5$/check_nrpe -H $HOSTADDRESS$ -n -c alias_check_applicationVersion -a '-process=JM4'
}

define command{
	command_name			nrpe_alias_check_version_am5
	command_line			$USER5$/check_nrpe -H $HOSTADDRESS$ -n -c alias_check_applicationVersion -a '-process=AM5'
}

####

Einbinden in NSClient++
=======================
Aktivieren der externen Scripte: nsclient.ini

[/modules]
; CheckExternalScripts - Execute external scripts
CheckExternalScripts = 1

[/settings/external scripts
; COMMAND ARGUMENT PROCESSING - This option determines whether or not the we will allow clients to specify arguments to commands that are executed.
allow arguments = true

[/settings/external scripts/alias]
;	Abgleich der Version eines laufenden Prozesses
;	z.B. -version=1.8.0.72 -process=JM4 -compare=all
alias_check_applicationVersion = checkAppVersion $ARG1$

[/settings/external scripts/scripts]
checkAppVersion = scripts\\checkAppVersion.exe $ARG1$