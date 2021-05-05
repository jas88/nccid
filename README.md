# nccid

Tooling for the National Covid-19 Chest Imaging Database

## Notes

Before running the first time, ensure all DICOM files have the Archive bit set:

`attrib +A *.dcm /S`

The uploader will clear this bit on each file after successful upload, so interrupted upload
batches can be resumed more efficiently (without re-uploading the files already done). This
mimics the old-school DOS semantics of the Archive bit for file backups!

CSV files will be converted to the appropriate NCCID-specific JSON format (which differs
between positive and negative results) and uploaded as separate S3 objects. Row numbers
are reported to the console after each row is uploaded, so an interrupted CSV upload can
be manually resumed if necessary.
