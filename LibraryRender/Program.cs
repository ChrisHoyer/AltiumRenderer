using System.Collections;
using System.Drawing;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using OriginalCircuit.AltiumSharp;
using OriginalCircuit.AltiumSharp.Drawing;
using IContainer = OriginalCircuit.AltiumSharp.IContainer;

class LibraryRender
{
    private object? LibData;
    private string? fileName_input;
    private string? fileName_output;

    private IContainer? _activeContainer;
    private Renderer? _renderer;

    private BufferedGraphics? _graphicsBuffer;
    private Bitmap? _bitmapBuffer;

    private int image_width = 500;
    private int image_height = 500;

    static void Main(string[] args)
    {

        LibraryRender main = new LibraryRender();

        main.SetArguments(args);

        main.LoadFile();

        main.RenderAll();

    }


    private void SetArguments(string[] args)
    {
        fileName_input = "files/Passives.SchLib"; //args[0];
        fileName_output = "files/Resistors - Chip.png";    //args[1];


    }

    private SchLib GetAssetsSchLib()
    {
        using (var reader = new SchLibReader())
        {
            return reader.Read("assets.schlib");
        }
    }

    private void LoadFile()
    {
        if (!File.Exists(fileName_input))
        {
            throw new FileNotFoundException($"The file '{fileName_input}' does not exist.");
        }

        if (Path.GetExtension(fileName_input).Equals(".pcblib", StringComparison.InvariantCultureIgnoreCase))
        {
            using var reader = new PcbLibReader();
            LibData = reader.Read(fileName_input);
        }
        else if (Path.GetExtension(fileName_input).Equals(".schlib", StringComparison.InvariantCultureIgnoreCase))
        {
            using var reader = new SchLibReader();
            LibData = reader.Read(fileName_input);
        }
        else
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        // Setup all references
        if (LibData is PcbLib pcbLib)
        {
            _renderer = new PcbLibRenderer();
            _activeContainer = pcbLib.Items.OfType<IContainer>().FirstOrDefault();
        }
        else if (LibData is SchLib schLib)
        {
            _renderer = new SchLibRenderer(schLib.Header, GetAssetsSchLib());
            _activeContainer = schLib.Items.OfType<IContainer>().FirstOrDefault();

        }
    }
   
    private void RenderAll()
    {
        if (LibData is IEnumerable libItems)
        {
            foreach (var component in libItems)
            {
                if (component is IComponent iComponent)
                {
                    fileName_output = "files/" + iComponent.Name.Replace("/", "_") + ".png";
                    _activeContainer = libItems.OfType<IContainer>().FirstOrDefault(container => container == component);
                    RenderImage();
                }
            }
        }
    }

    private void RenderImage()
    {
        if (_graphicsBuffer == null)
        {
            _bitmapBuffer = new Bitmap(image_width, image_height);
            _graphicsBuffer = BufferedGraphicsManager.Current.Allocate(Graphics.FromImage(_bitmapBuffer), new Rectangle(0, 0, image_width, image_height));
        }

        _renderer.Component = _activeContainer;

        using (var bitmap = new Bitmap(image_width, image_height))
        using (var target = Graphics.FromImage(bitmap))
        {

            _renderer.Render(_graphicsBuffer.Graphics, image_width, image_height, true);
            _graphicsBuffer.Render(target);

            bitmap.Save(fileName_output);
        }

    }


}
