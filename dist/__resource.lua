resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

--dependency 'MenuAPI'

files {
	--'@MenuAPI/MenuAPI.dll',
	'MenuAPI.dll',
	'config.ini'
}

client_scripts {
	'VStancer.Client.net.dll'
}

export 'SetVstancerPreset'
export 'GetVstancerPreset'