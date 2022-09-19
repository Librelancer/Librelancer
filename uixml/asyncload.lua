class asyncload : asyncload_Designer with Modal
{ 
   asyncload() { 
	base(); 
    this.ModalInit();
   } 
   showall()
   {
      this.Elements.loader.Visible = true;
   }
}

