namespace GameCube.AmusementVision.LZ
{
	internal class LzssEncoder
	{
		// Ring buffer of size N, with extra F-1 bytes to facilitate comparison
		byte[] ringBuf = new byte[LzssParameters.N + LzssParameters.F - 1];

		// Match position and length of the longest match. Set by InsertNode().
		int matchPosition, matchLength;

		// Binary search trees.
		int[] left = new int[LzssParameters.N + 1];
		int[] right = new int[LzssParameters.N + 257];
		int[] parent = new int[LzssParameters.N + 1];

		/// Initialize binary trees.
		void InitTree()
		{
			/* For i = 0 to N - 1, right[i] and left[i] will be the right and
		     * left children of node i. These nodes need not be initialized.
		     *
		     * Also, parent[i] is the parent of node i.
		     * These are initialized to NIL (= N), which stands for 'not used'.
		     *
		     * For i = 0 to 255, right[N + i + 1] is the root of the tree
		     * for strings that begin with character i. These are initialized
		     * to NIL. Note there are 256 trees.
		     */

			for (int i = LzssParameters.N + 1; i <= LzssParameters.N + 256; i++)
				right[i] = LzssParameters.NIL;

			for (int i = 0; i < LzssParameters.N; i++)
				parent[i] = LzssParameters.NIL;
		}

		/**
	     * Inserts string of length F, ringBuf[r..r+F-1], into one of the
	     * trees (ringBuf[r]'th tree) and returns the longest-match position
	     * and length via the global variables matchPosition and matchLength.
	     * If matchLength >= F, then removes the old node in favor of the new
	     * one, because the old one will be deleted sooner.
	     * Note r plays double role, as tree node and position in buffer.
	     */
		void InsertNode(int r)
		{
			int i, p, cmp;
			int keyIdx;

			cmp = 1; keyIdx = r; p = LzssParameters.N + 1 + ringBuf[keyIdx + 0];
			right[r] = left[r] = LzssParameters.NIL; matchLength = 0;
			for (; ; )
			{
				if (cmp >= 0)
				{
					if (right[p] != LzssParameters.NIL) p = right[p];
					else { right[p] = r; parent[r] = p; return; }
				}
				else
				{
					if (left[p] != LzssParameters.NIL) p = left[p];
					else { left[p] = r; parent[r] = p; return; }
				}
				for (i = 1; i < LzssParameters.F; i++)
					if ((cmp = ringBuf[keyIdx + i] - ringBuf[p + i]) != 0) break;
				if (i > matchLength)
				{
					matchPosition = p;
					if ((matchLength = i) >= LzssParameters.F) break;
				}
			}
			parent[r] = parent[p]; left[r] = left[p]; right[r] = right[p];
			parent[left[p]] = r; parent[right[p]] = r;
			if (right[parent[p]] == p) right[parent[p]] = r;
			else left[parent[p]] = r;
			parent[p] = LzssParameters.NIL;  /* remove p */
		}

		/**
	     * Deletes node p from tree.
	     */
		void DeleteNode(int p)
		{
			int q;

			if (parent[p] == LzssParameters.NIL) return;  /* not in tree */
			if (right[p] == LzssParameters.NIL) q = left[p];
			else if (left[p] == LzssParameters.NIL) q = right[p];
			else
			{
				q = left[p];
				if (right[q] != LzssParameters.NIL)
				{
					do { q = right[q]; } while (right[q] != LzssParameters.NIL);
					right[parent[q]] = left[q]; parent[left[q]] = parent[q];
					left[q] = left[p]; parent[left[p]] = q;
				}
				right[q] = right[p]; parent[right[p]] = q;
			}
			parent[q] = parent[p];
			if (right[parent[p]] == p) right[parent[p]] = q; else left[parent[p]] = q;
			parent[p] = LzssParameters.NIL;
		}

		public byte[] Encode(byte[] input)
		{
			List<byte> app = new List<byte>();
			int inputPos = 0;

			int len, r, s, last_matchLength, i;

			byte[] code_buf = new byte[17];
			int code_buf_ptr;
			byte mask;

			InitTree(); // Initialize trees

			/* code_buf[1..16] saves eight units of code,
	           and code_buf[0] works as eight flags,
	           "1" representing that the unit is an unencoded ubyte (1 byte),
	           "0" a position-and-length pair (2 bytes).
	           Thus, eight units require at most 16 bytes of code. */
			code_buf[0] = 0;
			code_buf_ptr = 1;
			mask = 1;

			s = 0; r = LzssParameters.N - LzssParameters.F;

			// Clear the buffer with any character that will appear often.
			for (i = s; i < r; i++)
				ringBuf[i] = LzssParameters.BUFF_INIT;

			// Read F bytes into the last F bytes of the buffer
			for (len = 0; len < LzssParameters.F && inputPos < input.Length; len++)
				ringBuf[r + len] = input[inputPos++];

			if (len == 0) // Text of size zero
				return null;

			/* Insert the F strings,
	           each of which begins with one or more 'space' characters.
	           Note	the order in which these strings are inserted.
	           This way, degenerate trees will be less likely to occur. */
			for (i = 1; i <= LzssParameters.F; i++)
				InsertNode(r - i);

			/* Finally, insert the whole string just read.
	           The variables matchLength and matchPosition are set. */
			InsertNode(r);

			do
			{
				// matchLength may be spuriously long near the end of text.
				if (matchLength > len)
					matchLength = len;

				if (matchLength < LzssParameters.THRESHOLD)
				{
					// Not long enough match. Send one byte.
					matchLength = 1;
					code_buf[0] |= mask; // 'send one byte' flag
					code_buf[code_buf_ptr++] = ringBuf[r];  // Send uncoded.
				}
				else
				{
					// Send position and length pair. Note matchLength >= THRESHOLD.
					code_buf[code_buf_ptr++] = (byte)matchPosition;
					code_buf[code_buf_ptr++] = (byte)
						(((matchPosition >> 4) & 0xf0)
					  | (matchLength - LzssParameters.THRESHOLD));
				}

				if ((mask <<= 1) == 0)
				{ // Dropped high bit -> Buffer is full
					for (i = 0; i < code_buf_ptr; i++)
					{
						app.Add(code_buf[i]);
					}

					code_buf[0] = 0;
					code_buf_ptr = 1;
					mask = 1;
				}

				last_matchLength = matchLength;
				for (i = 0; i < last_matchLength && inputPos < input.Length; i++)
				{
					// Delete old strings and read new bytes
					DeleteNode(s);
					ringBuf[s] = input[inputPos++];

					/* If the position is near the end of buffer,
			         * extend the buffer to make string comparison easier. */
					if (s < LzssParameters.F - 1)
						ringBuf[s + LzssParameters.N] = input[inputPos - 1];

					// Since this is a ring buffer, increment the position modulo N.
					s = (s + 1) % LzssParameters.N; r = (r + 1) % LzssParameters.N;
					InsertNode(r);  /* Register the string in ringBuf[r..r+F-1] */
				}

				// After the end of text, no need to read, but buffer may not be empty
				while (i++ < last_matchLength)
				{
					DeleteNode(s);
					s = (s + 1) % LzssParameters.N; r = (r + 1) % LzssParameters.N;
					if (--len != 0)
						InsertNode(r);
				}
			} while (len > 0);  /* until length of string to be processed is zero */

			if (code_buf_ptr > 1) // Send remaining code.
			{
				for (i = 0; i < code_buf_ptr; i++)
				{
					app.Add(code_buf[i]);
				}
			}

			return app.ToArray();
		}
	}
}
