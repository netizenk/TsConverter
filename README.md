# TsConverter
Windows service to convert/compress ts files, grabbed by your TV Tuner card, to desired output format utilizing Handbrake CLI executable.

 - Use TsConverter.exe install to install the service.
 - Edit TsConvert.exe.config to configure options such as your root grab directory, HandBrake CLI options and so on:

    <userSettings>
        <TsConverter.Properties.Settings>
            <setting name="RootDirectory" serializeAs="String">
                <value>D:\Media\TV\TV Shows</value>
            </setting>
            <setting name="ConvertOptions" serializeAs="String">
                <value>-e x265 -q 21 -B 192</value>
            </setting>
            <setting name="OutputFileType" serializeAs="String">
                <value>.mp4</value>
            </setting>
            <setting name="ExecutableName" serializeAs="String">
                <value>C:\Util\TsConverter\HandBrakeCLI.exe</value>
            </setting>
            <setting name="DeleteInputFile" serializeAs="String">
                <value>True</value>
            </setting>
            <setting name="IgnoreList" serializeAs="String">
                <value>Ignore</value>
            </setting>
        </TsConverter.Properties.Settings>
    </userSettings>
