fx_version 'adamant'
games { 'gta5' }
--dependency 'MenuAPI'

files {
	--'@MenuAPI/MenuAPI.dll',
	'MenuAPI.dll',
	'Newtonsoft.Json.dll',
	'config.json'
}

client_scripts {
	'VStancer.Client.net.dll'
}

export 'SetVstancerPreset'
export 'GetVstancerPreset'