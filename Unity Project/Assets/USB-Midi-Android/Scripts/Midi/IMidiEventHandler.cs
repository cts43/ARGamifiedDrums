public interface IMidiEventHandler
{
    void RawMidi(sbyte a, sbyte b, sbyte c);
    void NoteOn(int note, int velocity);
    void NoteOff(int note);
    void DeviceAttached(string deviceName);
    void DeviceDetached(string deviceName);
}
