resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

--dependency 'MenuAPI'

file 'config.ini'

client_scripts {
	--'@MenuAPI/MenuAPI.net.dll',
	'MenuAPI.net.dll',
	'Vstancer.Client.net.dll'
}

export 'SetVstancerPreset'
export 'GetVstancerPreset'