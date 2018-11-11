resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

--dependency 'NativeUI'

file 'config.ini'

client_scripts {
	--'@NativeUI/NativeUI.net.dll',
	'NativeUI.net.dll',
	'Vstancer.Client.net.dll'
}

export 'LoadVstancerConfig'