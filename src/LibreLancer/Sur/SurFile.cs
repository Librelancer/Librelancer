using System;
using System.IO;
using Jitter.LinearMath;
namespace LibreLancer.Sur
{
	//TODO: Sur reader is VERY incomplete & undocumented
	public class SurFile
	{
		const string VERS_TAG = "vers";

		public SurFile (Stream stream)
		{
			using (var reader = new BinaryReader (stream)) {
				if (reader.ReadTag () != VERS_TAG)
					throw new Exception ("Not a sur file");
				reader.ReadSingle (); //vers?
				while (stream.Position < stream.Length) {
					uint meshid = reader.ReadUInt32 ();
					uint tagcount = reader.ReadUInt32 ();
					while (tagcount-- > 0) {
						var tag = reader.ReadTag ();
						if (tag == "surf") {
							uint size = reader.ReadUInt32 (); //TODO: SUR - What is this?
							var surf = new Surface(reader);

						} else if (tag == "exts") {
							//TODO: SUR - What are exts used for?
							var min = new JVector (
								          reader.ReadSingle (),
								          reader.ReadSingle (),
								          reader.ReadSingle ()
							          );
							var max = new JVector (
								          reader.ReadSingle (),
								          reader.ReadSingle (),
								          reader.ReadSingle ()
							          );
						} else if (tag == "!fxd") {
							//TODO: SUR - WTF is this?!
						} else if (tag == "hpid") {
							//TODO: SUR - hpid. What does this do?
							uint count2 = reader.ReadUInt32 ();
							while (count2-- > 0) {
								uint mesh2 = reader.ReadUInt32 ();
							}
						}
					}
				}
			}
		}


	}
}

