fx_version 'adamant'
games { 'gta5' }
--dependency 'MenuAPI'

files {
	--'@MenuAPI/MenuAPI.dll',
	'MenuAPI.dll',
	'config.xml'
}

client_scripts {
	'VStancer.Client.net.dll'
}

export 'SetVstancerPreset'
export 'GetVstancerPreset'